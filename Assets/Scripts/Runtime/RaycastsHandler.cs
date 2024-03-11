using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class RaycastsHandler
{
    private static readonly bool enableLogs = false;

    private static readonly Allocator allocator = Allocator.Persistent;
    private static readonly int batchLength = 2500;

    private static NativeArray<RaycastCommand> commands;

    private static NativeArray<RaycastHit>[] results;
    private static NativeArray<JobHandle> jobs;

    public static bool first = true;
    private static bool totFrst = true;

    private static Transform[] points;

    private static int resultsLength; // this value is updated on job schedule

    public static float[] GetRaycasts(Transform[] t, float maxRayDist)
    {
        float[] data;

        if (first)
        {
            data = new float[t.Length];

            if (!totFrst)
            {
                commands.Dispose();
                jobs.Dispose();

                for (int i = 0; i < results.Length; i++) results[i].Dispose();
            }

            totFrst = false;
            first = false;
        }
        else data = CollectJobsResults(maxRayDist);

        SheduleJobs(t, maxRayDist);

        if (enableLogs) Debug.Log($"Collecting job ({data.Length})");

        return data;
    }

    private static void SheduleJobs(Transform[] t, float maxRayDist)
    {
        resultsLength = t.Length;

        commands = new NativeArray<RaycastCommand>(t.Length, allocator);
        for (int i = 0; i < t.Length; i++) commands[i] = new RaycastCommand(t[i].position, t[i].up, maxRayDist, 1 << 7);

        // -----
        int jobsCount = (int)Mathf.Floor(t.Length / batchLength) + 1;

        if (enableLogs) Debug.Log($"Creating job (c: {jobsCount} - {t.Length})");


        // -- PREPARE RESULTS ARRAYS --
        int rem = t.Length;
        results = new NativeArray<RaycastHit>[jobsCount];
        for (int i = 0; i < results.Length; i++)
        {
            int length = Mathf.Min(rem, batchLength);
            results[i] = new NativeArray<RaycastHit>(length, allocator);
            rem -= length;
        }

        // -- SHEDULE JOBS --
        rem = t.Length;
        jobs = new NativeArray<JobHandle>(jobsCount, allocator);
        for (int i = 0; i < jobsCount; i++)
        {
            int length = Mathf.Min(rem, batchLength);
            jobs[i] = RaycastCommand.ScheduleBatch(commands.GetSubArray(batchLength * i, length), results[i], 1);
            rem -= length;
        }
    }

    private static float[] CollectJobsResults(float maxRayDist)
    {
        JobHandle.CompleteAll(jobs);

        commands.Dispose();

        int aId = 0;

        float[] dists = new float[resultsLength];
        for (int i = 0; i < results.Length; i++)
        {
            for (int j = 0; j < results[i].Length; j++)
            {
                dists[aId] = GetRaycastDistance(results[i][j], maxRayDist);
                aId++;
            }

            results[i].Dispose();
        }

        jobs.Dispose();

        return dists;
    }

    public static float GetCarRaycast(Transform t, float maxDist)
    {
        Physics.Raycast(t.position, t.up, out RaycastHit hit, maxDist, 1 << 7);
        return GetRaycastDistance(hit, maxDist);
    }

    private static float GetRaycastDistance(RaycastHit hit, float maxDist)
    {
        return hit.collider != null ? hit.distance: maxDist;
    }
}
