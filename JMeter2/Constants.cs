using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inflectra.RemoteLaunch.Engines.JMeter2AutomationEngine
{
    /// <summary>
    /// Contains some of the constants and enumerations used by the automation engine
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The token reported by the automation and used to identify in SpiraTest
        /// </summary>
        public const string AUTOMATION_ENGINE_TOKEN = "JMeter2";

        /// <summary>
        /// The version number of the plugin.
        /// </summary>
        public const string AUTOMATION_ENGINE_VERSION = "3.0.0";

        /// <summary>
        /// The name of external automated testing system
        /// </summary>
        public const string EXTERNAL_SYSTEM_NAME = "JMeter 2.x";
    }
}
