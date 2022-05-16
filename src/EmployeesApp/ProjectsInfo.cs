using System.Collections.Generic;

namespace EmployeesApp
{
    internal class ProjectsInfo
    {
        public ProjectsInfo()
            => Projects = new Dictionary<int, HashSet<int>>();

        public Dictionary<int, HashSet<int>> Projects { get; }

        public void AddProject(int projectId, int employeeId)
        {
            HashSet<int> employeesOnProject;
            if (!Projects.TryGetValue(projectId, out employeesOnProject))
            {
                employeesOnProject = new HashSet<int>();
                Projects.Add(projectId, employeesOnProject);
            }
            employeesOnProject.Add(employeeId);
        }
    }
}
