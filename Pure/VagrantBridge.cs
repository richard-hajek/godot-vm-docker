using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class VagrantBridge
{
    private readonly Dictionary<Tuple<Computer, Computer>, string> _networks =
        new Dictionary<Tuple<Computer, Computer>, string>();

    private readonly Dictionary<Computer, ComputerStatus> _statuses = new Dictionary<Computer, ComputerStatus>();
    public readonly Dictionary<Computer, string> Containers = new Dictionary<Computer, string>();

    public void Begin()
    {
        VagrantController.VagrantPrepare();
    }

    public void PrepareComputer(Computer computer)
    {
        VagrantController.PrepareComputerDir(computer.Id, computer.Dockerfile);
        VagrantController.PreparePeripheralDir(computer.Id);

        foreach (var peripheral in computer.Peripherals)
            VagrantController.PreparePeripheral(computer.Id, peripheral.Identificator);

        VagrantController.DockerImageBuild(computer.Id, out var imageId);
        VagrantController.DockerContainerCreate(computer.Id, imageId, computer.Peripherals.Count > 0,
            out var containerId);

        Containers.Add(computer, containerId);
        _statuses.Add(computer, ComputerStatus.Prepared);
    }

    public void StartComputer(Computer computer)
    {
        VagrantController.DockerContainerStart(Containers[computer]);
        _statuses[computer] = ComputerStatus.Running;
    }

    public void AttachToComputer(Computer computer, out StreamWriter stdin, out StreamReader stdout,
        out StreamReader stderr, bool forceSTY = false)
    {
        _statuses[computer] = ComputerStatus.Connected;
        VagrantController.DockerContainerAttach(Containers[computer], out stdin, out stdout, out stderr, forceSTY);
    }

    public void DisconnectComputer(Computer computer)
    {
        _statuses[computer] = ComputerStatus.Running;
    }

    public void StopComputer(Computer computer)
    {
        _statuses[computer] = ComputerStatus.Prepared;
        VagrantController.DockerContainerStop(Containers[computer]);
    }

    public void DeleteComputer(Computer computer)
    {
        VagrantController.DockerContainerRemove(Containers[computer]);
        _statuses.Remove(computer);
    }

    public void Stop()
    {
        var pcs = _statuses.Keys.ToList();
        foreach (var pc in pcs)
        {
            if (_statuses[pc] == ComputerStatus.Connected)
                DisconnectComputer(pc);

            if (_statuses[pc] == ComputerStatus.Running)
                StopComputer(pc);

            DeleteComputer(pc);
        }

        VagrantController.VagrantStop();
    }

    public void ConnectComputers(Computer a, Computer b)
    {
        var tuple = new Tuple<Computer, Computer>(a, b);
        var netname = Convert.ToBase64String(new Guid().ToByteArray());
        _networks.Add(tuple, netname);

        Console.WriteLine("Network named " + netname);
        VagrantController.DockerNetworkCreate(netname);
        VagrantController.DockerNetworkConnect(Containers[a], netname);
        VagrantController.DockerNetworkConnect(Containers[b], netname);
    }

    public void DisconnectComputers(Computer a, Computer b)
    {
        var tuple = new Tuple<Computer, Computer>(a, b);
        var tupleRev = new Tuple<Computer, Computer>(b, a);

        var netname = _networks.ContainsKey(tuple) ? _networks[tuple] : _networks[tupleRev];

        VagrantController.DockerNetworkDisconnect(Containers[a], netname);
        VagrantController.DockerNetworkDisconnect(Containers[b], netname);
        VagrantController.DockerNetworkRemove(netname);
    }

    public void EavesdropOn(Computer a, Computer b, Computer eavesdropping)
    {
        var tuple = new Tuple<Computer, Computer>(a, b);
        var tupleRev = new Tuple<Computer, Computer>(b, a);
        var netname = _networks.ContainsKey(tuple) ? _networks[tuple] : _networks[tupleRev];

        VagrantController.DockerNetworkConnect(Containers[eavesdropping], netname);
        var ipa = VagrantController.GetContainerIP(Containers[a], netname);
        var ipb = VagrantController.GetContainerIP(Containers[b], netname);
        var ipAttacker = VagrantController.GetContainerIP(Containers[eavesdropping], netname);
        VagrantController.IptablesEavesdropOn(ipa, ipb, ipAttacker);
    }

    public void StopEavesdroppingOn(Computer a, Computer b, Computer eavesdropping)
    {
        var tuple = new Tuple<Computer, Computer>(a, b);
        var tupleRev = new Tuple<Computer, Computer>(b, a);
        var netname = _networks.ContainsKey(tuple) ? _networks[tuple] : _networks[tupleRev];

        VagrantController.DockerNetworkDisconnect(Containers[eavesdropping], netname);
        var ipa = VagrantController.GetContainerIP(Containers[a], netname);
        var ipb = VagrantController.GetContainerIP(Containers[b], netname);
        var ipAttacker = VagrantController.GetContainerIP(Containers[eavesdropping], netname);
        VagrantController.IptablesRemoveEavesdropping(ipa, ipb, ipAttacker);
    }

    public string GetComputerIP(Computer c)
    {
        var network = "";

        foreach (var pair in _networks)
        {
            var pc1 = pair.Key.Item1;
            var pc2 = pair.Key.Item2;
            var net = pair.Value;

            if (pc1 == c) network = net;

            if (pc2 == c) network = net;
        }

        if (network != "")
            return VagrantController.GetContainerIP(Containers[c], network);

        return "";
    }


    public void GetPeripheralStreams(Peripheral peripheral, out StreamReader ingoing, out StreamWriter outgoing)
    {
        ingoing = VagrantController.GetPeripheralInstream(peripheral.Identificator, peripheral.Parent.Id);
        outgoing = VagrantController.GetPeripheralOutstream(peripheral.Identificator, peripheral.Parent.Id);
    }

    private enum ComputerStatus
    {
        Prepared,
        Running,
        Connected
    }

    private static class VagrantController
    {
        private const string VirtualMachineImage = "ailispaw/barge";
        private const string InvagrantComputersMount = "/hackfest";
        private const string InvagrantDevicesFolder = "/v_dev";
        private const string IncontainerDevMount = "/dev/per";

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
            _inVagrantExecute($"cd {InvagrantComputersMount}/{path} && docker build -q ./", out _, out var stdout,
                out _);
            imageId = stdout.ReadLine();
        }

        public static void DockerContainerCreate(string pcPath, string imageId, bool mountDev, out string containerId)
        {
            StreamReader stdout;
            if (!mountDev)
                _inVagrantExecute($"docker create -it {imageId}", out _, out stdout, out _);
            else
                _inVagrantExecute(
                    $"docker create -it -v {InvagrantDevicesFolder}/{pcPath}/:{IncontainerDevMount} {imageId}", out _,
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

        private static void UpdateVagrantfile(string computersPath)
        {
            if (!VagrantFolderExists())
                Directory.CreateDirectory(GetVagrantPath());

            var vagrantfilePath = Path.Combine(GetVagrantPath(), "Vagrantfile");
            var vagrantfile = _generateVagrantfile(computersPath);

            File.WriteAllText(vagrantfilePath, vagrantfile);
        }

        private static string _generateVagrantfile(string computersPath)
        {
            return "Vagrant.configure(\"2\") do |config| \n" +
                   $"\tconfig.vm.box = \"{VirtualMachineImage}\" \n" +
                   $"\tconfig.vm.synced_folder \"{computersPath}\", \"{InvagrantComputersMount}\"\n" +
                   "end\n";
        }

        public static void DockerNetworkCreate(string netName)
        {
            _inVagrantExecute($"docker network create {netName}").WaitForExit();
        }

        public static void DockerNetworkConnect(string computerIdentifier, string netName)
        {
            _inVagrantExecute($"docker network connect {netName} {computerIdentifier}").WaitForExit();
        }

        public static void DockerNetworkDisconnect(string computerIdentifier, string netName)
        {
            _inVagrantExecute($"ssh -c \"docker network disconnect {netName} {computerIdentifier}").WaitForExit();
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

        public static void IptablesWipe()
        {
        }

        public static void PreparePeripheralDir(string pcPath)
        {
            _inVagrantExecute($"mkdir -p {InvagrantDevicesFolder}/{pcPath}").WaitForExit();
        }

        public static void PreparePeripheral(string pcPath, string id)
        {
            _inVagrantExecute($"mkdir {InvagrantDevicesFolder}/{pcPath}/{id}").WaitForExit();
            _inVagrantExecute($"mkfifo {InvagrantDevicesFolder}/{pcPath}/{id}/in").WaitForExit();
            _inVagrantExecute($"mkfifo {InvagrantDevicesFolder}/{pcPath}/{id}/out").WaitForExit();
        }

        public static void DevPrepare()
        {
            _inVagrantExecute($"sudo rm -r {InvagrantDevicesFolder}").WaitForExit();
            _inVagrantExecute($"sudo mkdir {InvagrantDevicesFolder}").WaitForExit();
            _inVagrantExecute($"sudo chmod 777 {InvagrantDevicesFolder}").WaitForExit();
        }

        public static StreamReader GetPeripheralInstream(string deviceId, string computerPath)
        {
            _inVagrantExecute($"cat {InvagrantDevicesFolder}/{computerPath}/{deviceId}/in", out _, out var stdout,
                out _);
            return stdout;
        }

        public static StreamWriter GetPeripheralOutstream(string deviceId, string computerPath)
        {
            _inVagrantExecute($"cat - > {InvagrantDevicesFolder}/{computerPath}/{deviceId}/out", out var stdin, out _,
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
            
            Console.WriteLine($"Executing: {sshProcess.StartInfo.FileName} {sshProcess.StartInfo.Arguments}");

            if (forceTTY)
                sshProcess.StartInfo.Arguments =
                    $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key -tt \"{command}\"";
            else
                sshProcess.StartInfo.Arguments =
                    $"-o UserKnownHostsFile=/dev/null -o \"StrictHostKeyChecking no\" bargee@127.0.0.1 -p 2222 -i .vagrant/machines/default/virtualbox/private_key \"{command}\"";

            Console.WriteLine($"[Executing] {sshProcess.StartInfo.FileName} {sshProcess.StartInfo.Arguments}");
            sshProcess.Start();

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
                    WorkingDirectory = GetVagrantPath()
                }
            };

            Console.WriteLine($"Executing: {sshProcess.StartInfo.FileName} {sshProcess.StartInfo.Arguments}");
            sshProcess.Start();

            return sshProcess;
        }

        public static void PrepareComputerDir(string id, string dockerfile)
        {
            var computerDirectory = Path.Combine(WorkingDirectory, id);
            if (Directory.Exists(computerDirectory))
                Directory.Delete(computerDirectory, true);

            Directory.CreateDirectory(computerDirectory);
            File.WriteAllText(Path.Combine(computerDirectory, "Dockerfile"), dockerfile);
        }
    }
}