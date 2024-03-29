﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Transcoder
{
	public static class TasksUtilities
	{
		/// <summary>
		/// Starts the given tasks and waits for them to complete. This will run, at most, the specified number of tasks in parallel.
		/// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
		/// </summary>
		/// <param name="tasksToRun">The tasks to run.</param>
		/// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, CancellationToken cancellationToken = new CancellationToken())
		{
			StartAndWaitAllThrottled(tasksToRun, maxActionsToRunInParallel, -1, cancellationToken);
		}

		/// <summary>
		/// Starts the given tasks and waits for them to complete. This will run the specified number of tasks in parallel.
		/// <para>NOTE: If a timeout is reached before the Task completes, another Task may be started, potentially running more than the specified maximum allowed.</para>
		/// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
		/// </summary>
		/// <param name="tasksToRun">The tasks to run.</param>
		/// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
		/// <param name="timeoutInMilliseconds">The maximum milliseconds we should allow the max tasks to run in parallel before allowing another task to start. Specify -1 to wait indefinitely.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, int timeoutInMilliseconds, CancellationToken cancellationToken = new CancellationToken())
		{
			// Convert to a list of tasks so that we don't enumerate over it multiple times needlessly.
			var tasks = tasksToRun.ToList();

			using (var throttler = new SemaphoreSlim(maxActionsToRunInParallel))
			{
				var postTaskTasks = new List<Task>();

				// Have each task notify the throttler when it completes so that it decrements the number of tasks currently running.
				tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => throttler.Release())));

				// Start running each task.
				foreach (var task in tasks)
				{
					// Increment the number of tasks currently running and wait if too many are running.
					throttler.Wait(timeoutInMilliseconds, cancellationToken);

					cancellationToken.ThrowIfCancellationRequested();
					task.Start();
				}

				// Wait for all of the provided tasks to complete.
				// We wait on the list of "post" tasks instead of the original tasks, otherwise there is a potential race condition where the throttler's using block is exited before some Tasks have had their "post" action completed, which references the throttler, resulting in an exception due to accessing a disposed object.
				Task.WaitAll(postTaskTasks.ToArray(), cancellationToken);
			}
		}

		/// <summary>
		/// Starts the given tasks and waits for them to complete. This will run, at most, the specified number of tasks in parallel.
		/// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
		/// </summary>
		/// <param name="tasksToRun">The tasks to run.</param>
		/// <param name="maxTasksToRunInParallel">The maximum number of tasks to run in parallel.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static async Task StartAndWaitAllThrottledAsync(IEnumerable<Task> tasksToRun, int maxTasksToRunInParallel, CancellationToken cancellationToken = new CancellationToken())
		{
			await StartAndWaitAllThrottledAsync(tasksToRun, maxTasksToRunInParallel, -1, cancellationToken);
		}

		/// <summary>
		/// Starts the given tasks and waits for them to complete. This will run the specified number of tasks in parallel.
		/// <para>NOTE: If a timeout is reached before the Task completes, another Task may be started, potentially running more than the specified maximum allowed.</para>
		/// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
		/// </summary>
		/// <param name="tasksToRun">The tasks to run.</param>
		/// <param name="maxTasksToRunInParallel">The maximum number of tasks to run in parallel.</param>
		/// <param name="timeoutInMilliseconds">The maximum milliseconds we should allow the max tasks to run in parallel before allowing another task to start. Specify -1 to wait indefinitely.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static async Task StartAndWaitAllThrottledAsync(IEnumerable<Task> tasksToRun, int maxTasksToRunInParallel, int timeoutInMilliseconds, CancellationToken cancellationToken = new CancellationToken())
		{
			// Convert to a list of tasks so that we don't enumerate over it multiple times needlessly.
			var tasks = tasksToRun.ToList();

			using (var throttler = new SemaphoreSlim(maxTasksToRunInParallel))
			{
				var postTaskTasks = new List<Task>();

				// Have each task notify the throttler when it completes so that it decrements the number of tasks currently running.
				tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => throttler.Release())));

				// Start running each task.
				foreach (var task in tasks)
				{
					// Increment the number of tasks currently running and wait if too many are running.
					await throttler.WaitAsync(timeoutInMilliseconds, cancellationToken);

					cancellationToken.ThrowIfCancellationRequested();
					task.Start();
				}

				// Wait for all of the provided tasks to complete.
				// We wait on the list of "post" tasks instead of the original tasks, otherwise there is a potential race condition where the throttler's using block is exited before some Tasks have had their "post" action completed, which references the throttler, resulting in an exception due to accessing a disposed object.
				await Task.WhenAll(postTaskTasks.ToArray());
			}
		}

		/// <summary>
		/// Starts the given Actions and waits for them to complete. This will run, at most, the specified number of Actions in parallel.
		/// </summary>
		/// <param name="actionsToRun">The actions to run.</param>
		/// <param name="maxActionsToRunInParallel">The maximum actions to run in parallel.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static void StartAndWaitAllThrottled(IEnumerable<Action> actionsToRun, int maxActionsToRunInParallel, CancellationToken cancellationToken = new CancellationToken())
		{
			var options = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = maxActionsToRunInParallel
			};

			Parallel.Invoke(options, actionsToRun.ToArray());
		}
	}
}
