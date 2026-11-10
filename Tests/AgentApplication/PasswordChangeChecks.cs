using Gurux.DLMS.AMI.Agent.Worker;

internal static class PasswordChangeChecks
{
    internal static async Task RunAsync()
    {
        var classify = typeof(GXAgentWorker).GetMethod("IsPasswordChange", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        foreach (var data in new[] { "42", "null", "[]", "{\"PasswordChangeId\":12}", "{\"PasswordChangeId\":null}" })
        {
            var task = new Gurux.DLMS.AMI.Shared.DTOs.GXTask { TaskType = Gurux.DLMS.AMI.Shared.Enums.TaskType.Write, Data = data };
            if ((bool)classify.Invoke(null, [task, Guid.Empty])!)
            {
                throw new Exception("An ordinary write was classified as a password operation.");
            }
        }
        foreach (var alreadyNew in new[] { false, true })
        {
            var calls = new List<string>();
            Task Call(string name) { calls.Add(name); return Task.CompletedTask; }
            await GXPasswordChangeExecution.RunAsync(alreadyNew,
                () => Call("intent"), () => Call("write"), () => Call("verify"), () => Call("confirm"));
            var expected = alreadyNew ? new[] { "confirm" } : new[] { "intent", "write", "verify", "confirm" };
            if (!calls.SequenceEqual(expected))
            {
                throw new Exception("Password rotation performed the wrong sequence.");
            }
        }
        foreach (var failure in new[] { "intent", "write", "verify", "confirm" })
        {
            var calls = new List<string>();
            Task Call(string name)
            {
                calls.Add(name);
                if (name == failure)
                {
                    throw new IOException("Simulated lost response.");
                }
                return Task.CompletedTask;
            }
            try
            {
                await GXPasswordChangeExecution.RunAsync(false,
                    () => Call("intent"), () => Call("write"), () => Call("verify"), () => Call("confirm"));
                throw new Exception("The failed password change was reported as successful.");
            }
            catch (IOException)
            {
                if (calls[^1] != failure)
                {
                    throw new Exception("Rotation continued after failure.");
                }
            }
        }
        // A lost acknowledgement must not reapply the meter write on retry.
        var writes = 0;
        var confirmations = 0;
        await GXPasswordChangeExecution.RunAsync(true, () => Task.CompletedTask,
            () => { writes++; return Task.CompletedTask; }, () => Task.CompletedTask,
            () => { confirmations++; return Task.CompletedTask; });
        if (writes != 0 || confirmations != 1)
        {
            throw new Exception("An already verified association repeated the password write.");
        }
        Console.WriteLine("PASS password write intent, write/verify/commit failures and uncertain-write recovery without duplicate writes.");
        await PasswordMeterChecks.RunAsync();
    }
}