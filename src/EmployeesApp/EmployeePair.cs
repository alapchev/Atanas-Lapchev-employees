using System.Collections.Generic;

namespace EmployeesApp
{
    internal class EmployeePair
    {
        public EmployeePair()
            => CommonProjects = new Dictionary<int, int>();

        public int TotalDaysWorkedTogether { get; set; }

        public Dictionary<int, int> CommonProjects { get; }

        public void AddCommonProject(int projectId)
        {
            if (!CommonProjects.TryAdd(projectId, 1))
            {
                CommonProjects[projectId]++;
            }
        }
    }
}
