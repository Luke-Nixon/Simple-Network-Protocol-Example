using System;
using System.Collections.Concurrent;


// https://stackoverflow.com/questions/41330771/use-unity-api-from-another-thread-or-call-a-function-in-the-main-thread

// so this is a hack to solve the issue of not being able to call unity functions from non main thread.
// i think if the issue persists. it might be better to just create a list of "handle_new_clients" stuff and run a "process new clients" bit in the update function.

public class ExecuteOnMainThread
{

    public static readonly ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();

    public static void Update()
    {
        if (!RunOnMainThread.IsEmpty)
        {
            while (RunOnMainThread.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}
