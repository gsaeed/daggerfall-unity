using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Wenzil.Console;

namespace Wenzil.Console.Commands
{ 
    /// <summary>
    /// HELP command. Display the list of available commands or details about a specific command.
    /// </summary>
    public static class HelpCommand
    {
        public static readonly string name = "HELP";
        public static readonly string description = "Display the list of available commands or details about a specific command.";
        public static readonly string usage = "HELP [command]";

        private static StringBuilder commandList = new StringBuilder();

        public static string Execute(params string[] args)
        {
            if (args.Length == 0)
            {
                return DisplayAvailableCommands();
            } if (args[0].Contains("*"))
            {
                return DisplaySomeCommands(args[0]);
            }
            else
            {
                return DisplayCommandDetails(args[0]);
            }
        }

        private static string DisplayAvailableCommands()
        {
            commandList.Length = 0; // clear the command list before rebuilding it
            commandList.Append("<b>Available Commands</b>\n");

            foreach (ConsoleCommand command in ConsoleCommandsDatabase.commands)
            {
                commandList.Append(string.Format("    <b>{0}</b> - {1}\n", command.name, command.description));
            }

            commandList.Append("To display details about a specific command, type 'HELP' followed by the command name.");
            return commandList.ToString();
        }

        private static string DisplaySomeCommands(string commandString)
        {
            commandList.Length = 0; // clear the command list before rebuilding it
            commandList.Append("<b>Available Commands</b>\n");
            string commandMatch = "";
            bool startsWith = false;
            for (int n = 0; n < commandString.Length; n++)
            {
                char a = commandString[n];
                if (a >= 'a' && a <= 'z')
                {
                    commandMatch += a;
                    if (n == 0)
                        startsWith = true;
                } else
                {
                    if (startsWith)
                        break;
                }

            }
            
            foreach (ConsoleCommand command in ConsoleCommandsDatabase.commands)
            {
                if (startsWith)
                {
                    if (command.name.ToLower().StartsWith(commandMatch))
                        commandList.Append(string.Format("    <b>{0}</b> - {1}\n", command.name, command.description));
                }
                else
                {
                if (command.name.ToLower().Contains(commandMatch))
                    commandList.Append(string.Format("    <b>{0}</b> - {1}\n", command.name, command.description));
                }

            }

            commandList.Append("To display details about a specific command, type 'HELP' followed by the command name.");
            return commandList.ToString();
        }

        private static string DisplayCommandDetails(string commandName)
        {
            string formatting =
@"<b>{0} Command</b>
    <b>Description:</b> {1}
    <b>Usage:</b> {2}";

            try
            {
                ConsoleCommand command = ConsoleCommandsDatabase.GetCommand(commandName);
                return string.Format(formatting, command.name, command.description, command.usage);
            }
            catch (NoSuchCommandException exception)
            {
                return string.Format("Cannot find help information about {0}. Are you sure it is a valid command?", exception.command);
            }
        }
    }
}
