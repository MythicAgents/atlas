using System;

namespace Atlas {
    class JobManagement {
        public static bool KillJob(Messages.JobList jobList, int jobNum)
        {
            try
            {
                int count = 0;
                foreach (Messages.Job job in jobList.jobs)
                {
                    if (job.job_id == jobNum)
                    {
                        jobList.jobs[jobNum].thread.Abort();
                        jobList.jobs.RemoveAt(count);
                        break;
                    }
                    count++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetJobs(Messages.JobList jobList)
        {
            string jobs = "Job ID\tTask ID\t\t\t\t\tCommand\t\tParameters\n------\t-------\t\t\t\t\t-------\t\t---------\n";
            foreach (Messages.Job job in jobList.jobs)
            {
                jobs += String.Format("{0}\t{1}\t{2}\t\t{3}\n", job.job_id, job.task_id, job.command, job.parameters.Replace(@"\", @""));
            }
            return jobs;
        }

        public static bool RemoveJob(Messages.Job Job, Messages.JobList JobList)
        {
            try
            {
                if (Job.command == "download")
                {
                    JobList.jobs.Remove(Job);
                }
                else
                {
                    Job.thread.Abort();
                    JobList.jobs.Remove(Job);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}