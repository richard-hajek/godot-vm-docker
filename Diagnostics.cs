using Godot;
using Godot.Collections;

public static class Diagnostics
{
    private static void VagrantTest(ContainerAPI containers)
    {
        var testingOutput = containers.VagrantDebugExecute("echo hello");

        if (testingOutput.StartsWith("hello")) // There might be some SSH output after this
        {
            GD.Print("[DIAGNOSTICS B] [OK] Vagrant box started successfully");
        }
        else
        {
            GD.PrintErr("[DIAGNOSTICS B] [ERROR] Vagrant box failed to start!");
        }
    }

    private static void ContainerTest(ContainerAPI containers)
    {
        var dockerfile1 =
            "FROM busybox\n" +
            "CMD [\"sh\"]";

        var customId = "1";
        var code = containers.CreateContainer(customId, null, dockerfile1, new Array<string>());

        if (code == (int) Errors.OK)
        {
            GD.Print("[DIAGNOSTICS B] [OK] Container created successfully.");
        }
        else
        {
            GD.PrintErr(
                $"[DIAGNOSTICS B] [ERROR] Container failed to create, with error: {(Errors) code} (Note that for now, BuildError means literally anything from failed SSH to bad Dockerfile)");
        }


        code = containers.StartContainer(customId);

        if (code == (int) Errors.OK)
        {
            GD.Print("[DIAGNOSTICS B] [OK] Container started successfully.");
        }
        else
        {
            GD.PrintErr($"[DIAGNOSTICS B] [ERROR] Container failed to start, with error: {(Errors) code}");
        }

        code = containers.CreateTTY(customId, out var stdin, out var stdout);

        if (code == (int) Errors.OK)
        {
            GD.Print("[DIAGNOSTICS B] [OK] Successfully created TTY.");
        }
        else
        {
            GD.PrintErr($"[DIAGNOSTICS B] [ERROR] Container failed to create TTY, with error: {(Errors) code}");
        }

        stdin.WriteLine("echo hello");
        stdin.Close();
        var received = stdout.ReadLine();

        if (received == "hello")
        {
            GD.Print("[DIAGNOSTICS B] [OK] Container responding");
        }
        else
        {
            GD.PrintErr("[DIAGNOSTICS B] [ERROR] Container not responding");
        }
    }

    public static void Run()
    {
        var containers = new ContainerAPI();
        containers.Begin();
        VagrantTest(containers);
        ContainerTest(containers);
        containers.Stop();
    }
}