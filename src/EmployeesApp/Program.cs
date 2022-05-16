using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeesApp
{
    internal class Program
    {
        private static int _maxDays;
        private static string[] _dateFormats;
        private static Dictionary<DateTime, ProjectsInfo> _dates;
        private static Dictionary<(int Employee1Id, int Employee2Id), EmployeePair> _employeePairs;
        private static List<(int Employee1Id, int Employee2Id)> _employeePairsWithMaxDays;

        static async Task Main(string[] args)
        {
            string path = GetPath(args);
            Initialize();
            await ReadEntries(path);
            FindEmployeePairs();
            string results = GetResults();
            Console.WriteLine(results);
        }

        private static string GetPath(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Incorrect number of arguments supplied");
                Console.WriteLine("Usage: EmployeesApp.exe <path-to-file>");
                Environment.Exit(0);
            }

            string path;
            if (args.Length == 1)
            {
                path = args[0];
            }
            else
            {
                Console.Write("Path to file: ");
                path = Console.ReadLine();
            }
            CheckPath(path);

            return Path.GetFullPath(path);
        }

        private static void CheckPath(string path)
        {
            bool isValid = true;
            if (!File.Exists(path))
            {
                Console.WriteLine("File doesn't exist");
                isValid = false;
            }
            else
            {
                string extension = Path.GetExtension(path)?.ToUpperInvariant();
                if (extension != ".CSV"
                    && extension != ".TXT"
                    && extension != string.Empty)
                {
                    Console.WriteLine("File extension should be .csv, .txt or none");
                    isValid = false;
                }
            }

            if (!isValid)
            {
                Environment.Exit(0);
            }
        }

        private static void Initialize()
        {
            _dates = new Dictionary<DateTime, ProjectsInfo>();
            _employeePairs = new Dictionary<(int, int), EmployeePair>();
            _employeePairsWithMaxDays = new List<(int, int)>();
            _dateFormats = new[]
            {
                "yyyy-M-d",
                "d-M-yyyy",
                "d/M/yyyy",
                "d.M.yyyy",
                "yyyyMMdd"
            };
        }

        private static async Task ReadEntries(string path)
        {
            string[] lines = null;
            try
            {
                lines = await File.ReadAllLinesAsync(path);
            }
            catch (Exception)
            {
                Console.WriteLine("Error reading from file");
                Environment.Exit(0);
            }

            if (lines is null || lines.Length == 0)
            {
                return;
            }

            int lineNumber = 0;
            string[] dateFormats = _dateFormats;
            if (!char.IsDigit(lines[0].TrimStart()[0]))
            {
                lineNumber = 1;
            }
            for (; lineNumber < lines.Length; lineNumber++)
            {
                ParseLine(lines[lineNumber], dateFormats);
            }
        }

        private static void ParseLine(string line, string[] dateFormats)
        {
            string[] tokens = line.Split(',');
            if (tokens.Length < 4)
            {
                return;
            }

            int employeeId;
            int projectId;
            DateTime startDate;
            DateTime endDate;

            if (!int.TryParse(tokens[0], out employeeId)
                || !int.TryParse(tokens[1], out projectId)
                || !DateTime.TryParseExact(tokens[2], dateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite, out startDate))
            {
                return;
            }

            if (tokens[3].Trim().ToUpperInvariant() == "NULL")
            {
                endDate = DateTime.Today;
            }
            else if (!DateTime.TryParseExact(tokens[3], dateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite, out endDate))
            {
                return;
            }

            if (startDate > endDate)
            {
                return;
            }

            int duration = (endDate - startDate).Days + 1;
            DateTime currDate = startDate;
            Dictionary<DateTime, ProjectsInfo> dates = _dates;
            for (int i = 0; i < duration; i++, currDate = currDate.AddDays(1))
            {
                ProjectsInfo projectsInfo;
                if (!dates.TryGetValue(currDate, out projectsInfo))
                {
                    projectsInfo = new ProjectsInfo();
                    dates.Add(currDate, projectsInfo);
                }
                projectsInfo.AddProject(projectId, employeeId);
            }
        }

        private static void FindEmployeePairs()
        {
            Dictionary<DateTime, ProjectsInfo> dates = _dates;
            Dictionary<(int, int), EmployeePair> employeePairs = _employeePairs;
            List<(int, int)> employeePairsWithMaxDays = _employeePairsWithMaxDays;

            foreach (var date in dates)
            {
                var employeePairsCurrentDate = new Dictionary<(int, int), EmployeePair>();

                foreach (var project in date.Value.Projects)
                {
                    List<int> employeesOnProject = project.Value.ToList();
                    for (int i = 0; i < employeesOnProject.Count - 1; i++)
                    {
                        for (int j = i + 1; j < employeesOnProject.Count; j++)
                        {
                            int employee1Id = employeesOnProject[i];
                            int employee2Id = employeesOnProject[j];
                            if (employee1Id > employee2Id)
                            {
                                int temp = employee1Id;
                                employee1Id = employee2Id;
                                employee2Id = temp;
                            }
                            (int, int) pairIds = (employee1Id, employee2Id);

                            EmployeePair employeePair;
                            if (!employeePairs.TryGetValue(pairIds, out employeePair))
                            {
                                employeePair = new EmployeePair();
                                employeePairs.Add(pairIds, employeePair);
                            }
                            employeePair.AddCommonProject(project.Key);
                            employeePairsCurrentDate.TryAdd(pairIds, employeePair);
                        }
                    }
                }

                foreach (var employeePairKvp in employeePairsCurrentDate)
                {
                    EmployeePair employeePair = employeePairKvp.Value;
                    employeePair.TotalDaysWorkedTogether++;
                    if (employeePair.TotalDaysWorkedTogether > _maxDays)
                    {
                        _maxDays = employeePair.TotalDaysWorkedTogether;
                        employeePairsWithMaxDays.Clear();
                        employeePairsWithMaxDays.Add(employeePairKvp.Key);
                    }
                    else if (employeePair.TotalDaysWorkedTogether == _maxDays)
                    {
                        employeePairsWithMaxDays.Add(employeePairKvp.Key);
                    }
                }
            }
        }

        private static string GetResults()
        {
            if (_maxDays == 0)
            {
                return "No employees have worked together on common projects";
            }

            var output = new StringBuilder();
            output.AppendLine("EmployeeID#1,EmployeeID#2,ProjectID,DaysWorked");

            foreach (var employeePairIds in _employeePairsWithMaxDays
                                .OrderBy(pair => pair.Employee1Id)
                                .ThenBy(pair => pair.Employee2Id))
            {
                var commonProjects = _employeePairs[employeePairIds]
                                     .CommonProjects.OrderBy(proj => proj.Key);
                foreach (var commonProject in commonProjects)
                {
                    output.AppendFormat("{0},{1},{2},{3}",
                        employeePairIds.Employee1Id, employeePairIds.Employee2Id,
                        commonProject.Key, commonProject.Value);
                    output.AppendLine();
                }
            }
            output.Replace(Environment.NewLine, null, output.Length - 2, 2);
            return output.ToString();
        }
    }
}
