using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public enum Errors
{
    OK,
    NoSuchContainer,
    NotConnected,
    AlreadyConnected,
    AlreadySnooping,
    NoSuchPeripheral,
    ImageBuildError
}

public class DockerBridge
{
    private readonly Dictionary<string, string> _containersIds = new Dictionary<string, string>();

    private readonly Dictionary<Tuple<string, string>, string> _networks =
        new Dictionary<Tuple<string, string>, string>();

    public void Begin()
    {
        VagrantController.VagrantPrepare();
    }

    public int CreateContainer(string customId, string dockerfile, IList<string> peripheralIds)
    {
        VagrantController.PrepareContainerDirectory(customId, dockerfile);
        VagrantController.PreparePeripheralDirectory(customId);

        foreach (var peripheral in peripheralIds)
            VagrantController.PreparePeripheral(customId, peripheral);

        VagrantController.DockerImageBuild(customId, out var imageId);

        if (string.IsNullOrEmpty(imageId))
            return (int) Errors.ImageBuildError;

        VagrantController.DockerContainerCreate(customId, imageId, peripheralIds.Count > 0, out var containerId);

        _containersIds.Add(customId, containerId);

        return (int) Errors.OK;
    }

    public int StartContainer(string customId)
    {
        if (!_containersIds.ContainsKey(customId)) return (int) Errors.NoSuchContainer;

        VagrantController.DockerContainerStart(_containersIds[customId]);

        return (int) Errors.OK;
    }

    public int CreateTTY(string customId, out StreamWriter stdin, out StreamReader stdout, bool forceSTY = false)
    {
        if (!_containersIds.ContainsKey(customId))
        {
            stdout = null;
            stdin = null;
            return (int) Errors.NoSuchContainer;
        }

        VagrantController.DockerContainerAttach(_containersIds[customId], out stdin, out stdout, out var stderr, forceSTY);
        //Logger.Log(stderr, "[AttachErr]"); This freezes the thing for some reason

        return (int) Errors.OK;
    }

    public int StopContainer(string customId)
    {
        if (!_containersIds.ContainsKey(customId)) return (int) Errors.NoSuchContainer;

        VagrantController.DockerContainerStop(_containersIds[customId]);

        return (int) Errors.OK;
    }

    public int DeleteContainer(string customId)
    {
        if (!_containersIds.ContainsKey(customId)) return (int) Errors.NoSuchContainer;

        VagrantController.DockerContainerRemove(_containersIds[customId]);

        return (int) Errors.OK;
    }

    public int Stop()
    {
        foreach (var pair in _containersIds)
        {
            var containerId = pair.Value;
            StopContainer(containerId);
            DeleteContainer(containerId);
        }

        VagrantController.VagrantStop();

        return (int) Errors.OK;
    }

    public int Connect(string containerIdA, string containerIdB)
    {
        if (!_containersIds.ContainsKey(containerIdA) || !_containersIds.ContainsKey(containerIdB))
            return (int) Errors.NoSuchContainer;

        var tuple = _determinableTuple(containerIdA, containerIdB);
        if (_networks.ContainsKey(tuple)) return (int) Errors.AlreadyConnected;

        var netName = Convert.ToBase64String(new Guid().ToByteArray());
        _networks.Add(tuple, netName);

        VagrantController.DockerNetworkCreate(netName);
        VagrantController.DockerNetworkConnect(_containersIds[containerIdA], netName);
        VagrantController.DockerNetworkConnect(_containersIds[containerIdB], netName);

        return (int) Errors.OK;
    }

    public int Disconnect(string containerIdA, string containerIdB)
    {
        var tuple = _determinableTuple(containerIdA, containerIdB);

        if (!_networks.ContainsKey(tuple)) return (int) Errors.NotConnected;

        var netname = _networks[tuple];

        VagrantController.DockerNetworkDisconnect(_containersIds[containerIdA], netname);
        VagrantController.DockerNetworkDisconnect(_containersIds[containerIdB], netname);
        VagrantController.DockerNetworkRemove(netname);
        return (int) Errors.OK;
    }
    public int SnoopOn(string containerIdA, string containerIdB, string containerSnooping)
    {
        var tuple = _determinableTuple(containerIdA, containerIdB);

        if (!_networks.ContainsKey(tuple))
        {
            return (int) Errors.NotConnected;
        }

        var netName = _networks[tuple];

        VagrantController.DockerNetworkConnect(_containersIds[containerSnooping], netName);
        var ipa = VagrantController.GetContainerIP(_containersIds[containerIdA], netName);
        var ipb = VagrantController.GetContainerIP(_containersIds[containerIdB], netName);
        var ipAttacker = VagrantController.GetContainerIP(_containersIds[containerSnooping], netName);
        VagrantController.IptablesEavesdropOn(ipa, ipb, ipAttacker);
        return (int) Errors.OK;
    }

    public int StopSnoopOn(string containerIdA, string containerIdB, string containerSnooping)
    {
        var tuple = _determinableTuple(containerIdA, containerIdB);

        if (!_networks.ContainsKey(tuple)) return (int) Errors.NotConnected;

        var netName = _networks[tuple];

        VagrantController.DockerNetworkDisconnect(_containersIds[containerSnooping], netName);
        var ipa = VagrantController.GetContainerIP(_containersIds[containerIdA], netName);
        var ipb = VagrantController.GetContainerIP(_containersIds[containerIdB], netName);
        var ipAttacker = VagrantController.GetContainerIP(_containersIds[containerSnooping], netName);
        VagrantController.IptablesRemoveEavesdropping(ipa, ipb, ipAttacker);
        return (int) Errors.OK;
    }
    
    private Tuple<string, string> _determinableTuple(string strA, string strB)
    {
        switch (string.CompareOrdinal(strA, strB))
        {
            case 1:
            case 0:
                return new Tuple<string, string>(strA, strB);
            case -1:
                return new Tuple<string, string>(strB, strA);
        }

        throw new Exception();
    }

    
    public StreamReader GetPeripheralIngoingStream(string containerId, string peripheralId)
    {
        return VagrantController.GetPeripheralInstream(peripheralId, containerId);
    }

    public StreamWriter GetPeripheralOutgoingStream(string containerId, string peripheralId)
    {
        return VagrantController.GetPeripheralOutstream(peripheralId, containerId);
    }

    private static class VagrantController
    {
        private const string VIRTUAL_MACHINE_IMAGE = "ailispaw/barge";
        private const string VAGRANT_CONTAINERS_MOUNT = "/hackfest";
        private const string VAGRANT_DEVICES_FOLDER = "/v_dev";
        private const string CONTAINER_DEV_MOUNT = "/dev/per";

        private static readonly string WorkingDirectory = Path.Combine(GameFiles.UserDirectoryPath, "map");

        public static void VagrantPrepare()
        {
            if (Directory.Exists(WorkingDirectory))
                Directory.Delete(WorkingDirectory, true);

            Directory.CreateDirectory(WorkingDirectory);

            if (!VagrantFolderExists())
            {
                UpdateVagrantfile(WorkingDirectory);
            }
            else if (!VagrantFileSufficient())
            {
                HaltVagrantBox();
                UpdateVagrantfile(WorkingDirectory);
            }

            StartVagrantBox();
            DockerWipe();
            DevPrepare();
        }

        public static void VagrantStop()
        {
            DockerWipe();
            HaltVagrantBox();
        }

        private static void StartVagrantBox()
        {
            _command("vagrant", "up", true);
        }

        private static void HaltVagrantBox()
        {
            _command("vagrant", "halt", true);
        }

        public static void DockerImageBuild(string path, out string imageId)
        {
            _inVagrantExecute($"cd {VAGRANT_CONTAINERS_MOUNT}/{path} && docker build -q ./", out _, out var stdout,
                out var stderr);
            imageId = stdout.ReadLine();

            Logger.Log(stdout, "build");
            Logger.Log(stderr, "build");
        }

        public static void DockerContainerCreate(string pcPath, string imageId, bool mountDev, out string containerId)
        {
            StreamReader stdout;
            if (!mountDev)
                _inVagrantExecute($"docker create -it {imageId}", out _, out stdout, out _);
            else
                _inVagrantExecute(
                    $"docker create -it -v {VAGRANT_DEVICES_FOLDER}/{pcPath}/:{CONTAINER_DEV_MOUNT} {imageId}", out _,
                    out stdout, out _);
            containerId = stdout.ReadLine();
        }

        public static void DockerContainerStart(string containerId)
        {
            _inVagrantExecute($"docker start {containerId}").WaitForExit();
        }

        public static void DockerContainerAttach(string containerId, out StreamWriter stdin, out StreamReader stdout,
            out StreamReader stderr, bool forceTty)
        {
            if (forceTty)
                _inVagrantExecute($"docker exec -it {containerId} sh", out stdin, out stdout, out stderr, true);
            else
                _inVagrantExecute($"docker exec -i {containerId} sh", out stdin, out stdout, out stderr);
        }

        public static void DockerContainerStop(string containerId)
        {
            _inVagrantExecute($"docker kill {containerId}").WaitForExit();
        }

        public static void DockerContainerRemove(string containerId)
        {
            _inVagrantExecute($"docker rm {containerId}").WaitForExit();
        }

        private static void DockerWipe()
        {
            _inVagrantExecute("docker rm -f $(docker ps -aq)").WaitForExit();
            _inVagrantExecute("docker rmi -f $(docker images -aq)").WaitForExit();
            _inVagrantExecute("docker network rm $(docker network ls -q --filter type=custom)").WaitForExit();
        }

        private static bool VagrantFolderExists()
        {
            return Directory.Exists(GetVagrantPath());
        }

        private static string GetVagrantPath()
        {
            var path = Path.Combine(GameFiles.UserDirectoryPath, "vagrant");
            return path;
        }

        private static bool VagrantFileSufficient()
        {
            var vboxPath = GetVagrantPath();
            var vagrantfilePath = Path.Combine(vboxPath, "Vagrantfile");

            if (!File.Exists(vagrantfilePath))
                return false;

            var current = File.ReadAllText(vagrantfilePath);
            var wouldBeNew = _generateVagrantfile(WorkingDirectory);

            return current == wouldBeNew;
        }

        private static void UpdateVagrantfile(string containerPath)
        {
            if (!VagrantFolderExists())
                Directory.CreateDirectory(GetVagrantPath());

            var vagrantfilePath = Path.Combine(GetVagrantPath(), "Vagrantfile");
            var vagrantfile = _generateVagrantfile(containerPath);

            File.WriteAllText(vagrantfilePath, vagrantfile);
        }

        private static string _generateVagrantfile(string containerPath)
        {
            return "Vagrant.configure(\"2\") do |config| \n" +
                   $"\tconfig.vm.box = \"{VIRTUAL_MACHINE_IMAGE}\" \n" +
                   $"\tconfig.vm.synced_folder \"{containerPath}\", \"{VAGRANT_CONTAINERS_MOUNT}\"\n" +
                   "end\n";
        }

        public static void DockerNetworkCreate(string netName)
        {
            _inVagrantExecute($"docker network create {netName}").WaitForExit();
        }

        public static void DockerNetworkConnect(string containerId, string netName)
        {
            _inVagrantExecute($"docker network connect {netName} {containerId}").WaitForExit();
        }

        public static void DockerNetworkDisconnect(string containerId, string netName)
        {
            _inVagrantExecute($"ssh -c \"docker network disconnect {netName} {containerId}").WaitForExit();
        }

        public static void DockerNetworkRemove(string netName)
        {
            _inVagrantExecute($"docker network rm {netName}").WaitForExit();
        }

        public static void IptablesEavesdropOn(string ip1, string ip2, string eavesdroppingIP)
        {
            _inVagrantExecute($"sudo iptables -I FORWARD -s {ip1} -j TEE --gateway {eavesdroppingIP}").WaitForExit();
            _inVagrantExecute($"sudo iptables -I FORWARD -s {ip2} -j TEE --gateway {eavesdroppingIP}").WaitForExit();
        }

        public static void IptablesRemoveEavesdropping(string ip1, string ip2, string eavesdroppingIP)
        {
            _inVagrantExecute($"sudo iptables -D FORWARD -s {ip1} -j TEE --gateway {eavesdroppingIP}").WaitForExit();
            _inVagrantExecute($"sudo iptables -D FORWARD -s {ip2} -j TEE --gateway {eavesdroppingIP}").WaitForExit();
        }

        public static void PreparePeripheralDirectory(string pcPath)
        {
            _inVagrantExecute($"mkdir -p {VAGRANT_DEVICES_FOLDER}/{pcPath}").WaitForExit();
        }

        public static void PreparePeripheral(string pcPath, string id)
        {
            _inVagrantExecute($"mkdir {VAGRANT_DEVICES_FOLDER}/{pcPath}/{id}").WaitForExit();
            _inVagrantExecute($"mkfifo {VAGRANT_DEVICES_FOLDER}/{pcPath}/{id}/in").WaitForExit();
            _inVagrantExecute($"mkfifo {VAGRANT_DEVICES_FOLDER}/{pcPath}/{id}/out").WaitForExit();
        }

        public static void DevPrepare()
        {
            _inVagrantExecute($"sudo rm -r {VAGRANT_DEVICES_FOLDER}").WaitForExit();
            _inVagrantExecute($"sudo mkdir {VAGRANT_DEVICES_FOLDER}").WaitForExit();
            _inVagrantExecute($"sudo chmod 777 {VAGRANT_DEVICES_FOLDER}").WaitForExit();
        }

        public static StreamReader GetPeripheralInstream(string peripheralId, string customId)
        {
            _inVagrantExecute($"cat {VAGRANT_DEVICES_FOLDER}/{customId}/{peripheralId}/in", out _, out var stdout,
                out _);
            return stdout;
        }

        public static StreamWriter GetPeripheralOutstream(string peripheralId, string customId)
        {
            _inVagrantExecute($"cat - > {VAGRANT_DEVICES_FOLDER}/{customId}/{peripheralId}/out", out var stdin, out _,
                out _);
            return stdin;
        }

        public static string GetContainerIP(string containerId, string networkId)
        {
            _inVagrantExecute($"docker inspect -f '{{.NetworkSettings.Networks.{networkId}.IPAddress}}' {containerId}",
                out _, out var stdout, out _);
            return stdout.ReadLine();
        }

        private static void _command(string command, string args, bool wait)
        {
            Console.WriteLine("[Executing] " + command + " " + string.Join(" ", args));
            var myProcess = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    WorkingDirectory = GetVagrantPath()
                }
            };
            myProcess.Start();
            Logger.WriteLine(
                $"Executing: {myProcess.StartInfo.FileName} {myProcess.StartInfo.Arguments} with PID {myProcess.Id}");

            if (wait) myProcess.WaitForExit();
        }

        private static Process _inVagrantExecute(string command, out StreamWriter stdin, out StreamReader stdout,
            out StreamReader stderr, bool forceTTY = false)
        {
            var sshProcess = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "ssh",
                    Arguments =
                        $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key \"{command}\"",
                    WorkingDirectory = GetVagrantPath(),
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }
            };

            if (forceTTY)
                sshProcess.StartInfo.Arguments =
                    $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key -tt \"{command}\"";
            else
                sshProcess.StartInfo.Arguments =
                    $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key \"{command}\"";

            sshProcess.Start();
            Logger.WriteLine(
                $"Executing: {sshProcess.StartInfo.FileName} {sshProcess.StartInfo.Arguments} with PID {sshProcess.Id}");

            stdout = sshProcess.StandardOutput;
            stdin = sshProcess.StandardInput;
            stderr = sshProcess.StandardError;

            return sshProcess;
        }

        private static Process _inVagrantExecute(string command)
        {
            var sshProcess = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = "ssh",
                    Arguments =
                        $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key \"{command}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = GetVagrantPath()
                }
            };

            sshProcess.Start();
            Logger.WriteLine(
                $"Executing: {sshProcess.StartInfo.FileName} {sshProcess.StartInfo.Arguments} with PID {sshProcess.Id}");
            Logger.Log(sshProcess.StandardError, $"{sshProcess.Id}");
            Logger.Log(sshProcess.StandardOutput, $"{sshProcess.Id}");

            return sshProcess;
        }

        public static void PrepareContainerDirectory(string id, string dockerfile)
        {
            var containerDirectory = Path.Combine(WorkingDirectory, id);
            if (Directory.Exists(containerDirectory))
                Directory.Delete(containerDirectory, true);

            Directory.CreateDirectory(containerDirectory);
            File.WriteAllText(Path.Combine(containerDirectory, "Dockerfile"), dockerfile);
        }
    }
}