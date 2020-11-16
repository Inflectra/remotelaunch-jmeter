using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Inflectra.RemoteLaunch.Interfaces;
using Inflectra.RemoteLaunch.Interfaces.DataObjects;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Inflectra.RemoteLaunch.Engines.JMeter2AutomationEngine
{
    /// <summary>
    /// Sample data-synchronization provider that synchronizes incidents between SpiraTest/Plan/Team and an external system
    /// </summary>

    /// <summary>
    /// Sample test automation engine plugin that implements the IAutomationEngine class.
    /// This class is instantiated by the RemoteLaunch application
    /// </summary>
    /// <remarks>
    /// The AutomationEngine class provides some of the generic functionality
    /// </remarks>
    public class JMeterAutomationEngine : AutomationEngine, IAutomationEngine
    {
        private const string CLASS_NAME = "JMeterAutomationEngine";

        /// <summary>
        /// Constructor
        /// </summary>
        public JMeterAutomationEngine()
        {
            //Set status to OK
            base.status = EngineStatus.OK;
        }


        /// <summary>
        /// Returns the author of the test automation engine
        /// </summary>
        public override string ExtensionAuthor
        {
            get
            {
                return "Inflectra Corporation";
            }
        }

        /// <summary>
        /// The unique GUID that defines this automation engine
        /// </summary>
        public override Guid ExtensionID
        {
            get
            {
                return new Guid("{5C87B5F7-74E8-4662-862E-F3DC3FAD338F}");
            }
        }

        /// <summary>
        /// Returns the display name of the automation engine
        /// </summary>
        public override string ExtensionName
        {
            get
            {
                return "Apache JMeter 2.x Automation Engine";
            }
        }

        /// <summary>
        /// Returns the unique token that identifies this automation engine to SpiraTest
        /// </summary>
        public override string ExtensionToken
        {
            get
            {
                return Constants.AUTOMATION_ENGINE_TOKEN;
            }
        }

        /// <summary>
        /// Returns the version number of this extension
        /// </summary>
        public override string ExtensionVersion
        {
            get
            {
                return Constants.AUTOMATION_ENGINE_VERSION;
            }
        }

        /// <summary>
        /// Adds a custom settings panel for allowing the user to set any engine-specific configuration values
        /// </summary>
        /// <remarks>
        /// 1) If you don't have any engine-specific settings, just comment out the entire Property
        /// 2) The SettingPanel needs to be implemented as a WPF XAML UserControl
        /// </remarks>
        public override System.Windows.UIElement SettingsPanel
        {
            get
            {
                return new AutomationEngineSettingsPanel();
            }
            set
            {
                AutomationEngineSettingsPanel settingsPanel = (AutomationEngineSettingsPanel)value;
                settingsPanel.SaveSettings();
            }
        }

        /// <summary>
        /// This is the main method that is used to start automated test execution
        /// </summary>
        /// <param name="automatedTestRun">The automated test run object</param>
        /// <returns>Either the populated test run or an exception</returns>
        public override AutomatedTestRun StartExecution(AutomatedTestRun automatedTestRun)
        {
            //Set status to OK
            base.status = EngineStatus.OK;
            string path = "";

            try
            {
                if (Properties.Settings.Default.TraceLogging)
                {
                    LogEvent("Starting test execution", EventLogEntryType.Information);
                }
                DateTime startDate = DateTime.Now;

                //See if we have any parameters we need to pass to the automation engine
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                if (automatedTestRun.Parameters == null)
                {
                    if (Properties.Settings.Default.TraceLogging)
                    {
                        LogEvent("Test Run has no parameters", EventLogEntryType.Information);
                    }
                }
                else
                {
                    if (Properties.Settings.Default.TraceLogging)
                    {
                        LogEvent("Test Run has parameters", EventLogEntryType.Information);
                    }

                    foreach (TestRunParameter testRunParameter in automatedTestRun.Parameters)
                    {
                        string parameterName = testRunParameter.Name.ToLowerInvariant();
                        if (!parameters.ContainsKey(parameterName))
                        {
                            //Make sure the parameters are lower case
                            if (Properties.Settings.Default.TraceLogging)
                            {
                                LogEvent("Adding test run parameter " + parameterName + " = " + testRunParameter.Value, EventLogEntryType.Information);
                            }
                            parameters.Add(parameterName, testRunParameter.Value);
                        }
                    }
                }

                //We store the script output in a temp file in the remote launch folder
                string outputFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\RemoteLaunch";
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                string outputFile = Path.Combine(outputFolder, "JMeterEngine_Output.log");

                //Delete the file if it already exists
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                //See if we have an attached or linked test script
                if (automatedTestRun.Type == AutomatedTestRun.AttachmentType.URL)
                {
                    //The "URL" of the test is actually the full file path of the file that contains the test script
                    //Some automation engines need additional parameters which can be provided by allowing the test script filename
                    //to consist of multiple elements separated by a specific character.
                    //Conventionally, most engines use the pipe (|) character to delimit the different elements
                    
                        //See if we have any pipes in the 'filename' that contains arguments or parameters
                        string[] filenameElements = automatedTestRun.FilenameOrUrl.Split('|');

                        //To make it easier, we have certain shortcuts that can be used in the path
                      //  string path = automatedTestRun.FilenameOrUrl;
                        path = filenameElements[0];
                        string arguments = "";

                        //To make it easier, we have certain shortcuts that can be used in the path
                        //This allows the same test to be run on different machines with different physical folder layouts
                        path = path.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                        path = path.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                        path = path.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                        path = path.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                        path = path.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));

                        //See if we have any arguments (not parameters)
                            
                        if (filenameElements.Length > 1)
                        {
                            //Replace any special folders in the arguments as well
                            arguments = filenameElements[1];
                            arguments = arguments.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                            arguments = arguments.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                            arguments = arguments.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                            arguments = arguments.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                            arguments = arguments.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));

                            //Replace the special test case and test run id tokens
                            arguments = arguments.Replace("[TestCaseId]", automatedTestRun.TestCaseId.ToString());
                            arguments = arguments.Replace("[TestRunId]", automatedTestRun.TestRunId.ToString());
                            if (automatedTestRun.TestSetId.HasValue)
                            {
                                arguments = arguments.Replace("[TestSetId]", automatedTestRun.TestSetId.Value.ToString());
                            }
                            if (automatedTestRun.ReleaseId.HasValue)
                            {
                                arguments = arguments.Replace("[ReleaseId]", automatedTestRun.ReleaseId.Value.ToString());
                            }
                        }
                        
                    //First make sure that the file exists
                    if (File.Exists(path))
                    {
                        if (Properties.Settings.Default.TraceLogging)
                        {
                            LogEvent("Executing " + Constants.EXTERNAL_SYSTEM_NAME + " test located at " + path, EventLogEntryType.Information);
                        }

                        //Construct the command line arguments and working folder
                        string commandDirectory = Properties.Settings.Default.Location;
                        string command = "jmeter.bat";
                        string commandArgs = "-n "; //Run in non-GUI mode

                        //Add on the test file
                        commandArgs += "-t \"" + path + "\" ";

                        //Add on the output file
                        commandArgs += "-l \"" + outputFile + "\" ";

                        //Add other arguments
                        commandArgs += " " + arguments + " ";

                        //Add on any parameters
                        if (parameters != null && parameters.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> parameter in parameters)
                            {
                                //Escape double-quotes (i.e. " --> "")
                                string paramName = parameter.Key;
                                if (paramName.Contains('"'))
                                {
                                    paramName = paramName.Replace("\"", "\"\"");
                                }
                                string paramValue = parameter.Value;
                                if (paramValue.Contains('"'))
                                {
                                    paramValue = paramValue.Replace("\"", "\"\"\"");
                                }
                                commandArgs += "-J" + paramName + "=\"" + paramValue + "\" ";
                            }
                        }

                        //Make sure the exe exists
                        string jmeterPath = Path.Combine(commandDirectory, command);
                        if (!File.Exists(jmeterPath))
                        {
                            throw new FileNotFoundException("Unable to find " + Constants.EXTERNAL_SYSTEM_NAME + " at " + jmeterPath);
                        }

                        //Execute the JMeter command line:
                        //jmeter.exe -n -t [test path] -l [log file path] -J [paramName=paramValue] [arguments]
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = jmeterPath;
                        startInfo.Arguments = commandArgs;
                        startInfo.WorkingDirectory = commandDirectory;
                        startInfo.UseShellExecute = false;
                        startInfo.ErrorDialog = false;

                        //Start the process and wait until completed
                        Process process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();
                        process.Close();
                    }
                    else
                    {
                        throw new FileNotFoundException("Unable to find a " + Constants.EXTERNAL_SYSTEM_NAME + " test at " + path);
                    }
                }
                else
                {
                    //The JMeter automation engine doesn't support embedded/attached scripts, so we throw the following exception:
                    throw new InvalidOperationException("The " + Constants.EXTERNAL_SYSTEM_NAME + " automation engine only supports linked test scripts");
                }

                //Capture the time that it took to run the test
                DateTime endDate = DateTime.Now;

                //Now extract the test results
                //Make sure that the output file exists
                if (!File.Exists(outputFile))
                {
                        throw new FileNotFoundException("Unable to find an output log for " + Constants.EXTERNAL_SYSTEM_NAME + " at " + outputFile);
                }
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(outputFile);
                }
                catch (XmlException)
                {
                    //Happens if the file is not XML or the XML is not well formed
                    string outputFileText = System.IO.File.ReadAllText(outputFile);
                    throw new ApplicationException("Unable to parse the " + Constants.EXTERNAL_SYSTEM_NAME + " XML Output file '" + outputFile + "' - " + outputFileText);
                }

                //Extract the results from the XML log-file
                //Default to N/A in case there are no assertions.
                TestRun.TestStatusEnum executionStatus = TestRun.TestStatusEnum.NotApplicable;
                string externalTestDetailedResults = "";
 
                XmlNodeList xmlAssertionResults = xmlDoc.SelectNodes("//assertionResult");
                //Loop through each result node
                int failureCount = 0;
                int errorCount = 0;

                if (xmlAssertionResults.Count > 0)
                {
                    //As long as we have one result, mark as passed
                    executionStatus = TestRun.TestStatusEnum.Passed;
                }
                foreach (XmlNode xmlAssertionResult in xmlAssertionResults)
                {
                    //Get the URL being accessed
                    string label = "";
                    label = xmlAssertionResult.ParentNode.Attributes["lb"].Value;
                    if (xmlAssertionResult.ParentNode.Attributes["lb"] != null)
                     {
                         label = xmlAssertionResult.ParentNode.Attributes["lb"].Value;
                     }
                    if (xmlAssertionResult != null)
                    {
                        string name = label;
                        if (xmlAssertionResult.SelectSingleNode("name") != null)
                        {
                            if (String.IsNullOrEmpty(label))
                            {
                                name = xmlAssertionResult.SelectSingleNode("name").InnerText;
                            }
                            else
                            {
                                name = xmlAssertionResult.SelectSingleNode("name").InnerText + " (" + label + ")";
                            }
                        }
                        string failure = "false";
                        if (xmlAssertionResult.SelectSingleNode("failure") != null)
                        {
                            failure = xmlAssertionResult.SelectSingleNode("failure").InnerText;
                        }
                        string error = "false";
                        if (xmlAssertionResult.SelectSingleNode("error") != null)
                        {
                            error = xmlAssertionResult.SelectSingleNode("error").InnerText;
                        }
                        string failureMessage = "";
                        if (xmlAssertionResult.SelectSingleNode("failureMessage") != null)
                        {
                            failureMessage = xmlAssertionResult.SelectSingleNode("failureMessage").InnerText;
                        }

                        if (!String.IsNullOrEmpty(failure) && failure.ToLowerInvariant() == "true")
                        {
                            failureCount++;
                            executionStatus = TestRun.TestStatusEnum.Failed;
                        }
                        if (!String.IsNullOrEmpty(error) && error.ToLowerInvariant() == "true")
                        {
                            errorCount++;
                            executionStatus = TestRun.TestStatusEnum.Failed;
                        }

                        string message = String.Format("{0}: failure={1}, error={2}, message='{3}'", name, failure, error, failureMessage);
                        externalTestDetailedResults += message + "\n";
                    }
                }


                string externalTestSummary = String.Format("Ran with {0} failures and {1} errors", failureCount, errorCount);

                //Populate the Test Run object with the results
                if (String.IsNullOrEmpty(automatedTestRun.RunnerName))
                {
                    automatedTestRun.RunnerName = this.ExtensionName.Substring(0,13);
                }
                automatedTestRun.RunnerTestName = Path.GetFileNameWithoutExtension(path);
                //Specify the start/end dates
                automatedTestRun.StartDate = startDate;
                automatedTestRun.EndDate = endDate;

                //The result log
                automatedTestRun.ExecutionStatus = executionStatus;
                automatedTestRun.RunnerMessage = externalTestSummary;
                automatedTestRun.RunnerStackTrace = externalTestDetailedResults;

                //Report as complete               
                base.status = EngineStatus.OK;
                return automatedTestRun;
            }
            catch (Exception exception)
            {
                //Log the error and denote failure
                LogEvent(exception.Message + " (" + exception.StackTrace + ")", EventLogEntryType.Error);
                //Report as completed with error
                base.status = EngineStatus.Error;
                throw exception;
            }
        }
    }
}
