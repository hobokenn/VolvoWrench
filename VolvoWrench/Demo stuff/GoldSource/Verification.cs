using MoreLinq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VolvoWrench.Demo_stuff.GoldSource;

namespace VolvoWrench.Demo_Stuff.GoldSource
{
    /// <summary>
    ///     This is a form to aid goldsource run verification
    /// </summary>
    public sealed partial class Verification : Form
    {
        public static Dictionary<string, CrossParseResult> Df = new Dictionary<string, CrossParseResult>();

        /// <summary>
        ///     This list contains the paths to the demos
        /// </summary>
        public List<string> DemopathList;

        /// <summary>
        ///     Static color definitions
        /// </summary>
        public static readonly Color DefaultColor = Color.White;
        public static readonly Color IllegalColor = Color.LightCoral;
        public static readonly Color WarningColor = Color.Yellow;
        public static readonly Color GoodColor = Color.Green;

        /// <summary>
        ///     Buffer holds strings to be printed
        /// </summary>
        private ColoredTextBuffer textBuffer;

        /// <summary>
        ///     Dictionaries related to HL100 kill counting
        /// </summary>
        // Kill Number : UUID, Monster Type, Monster Name, Map
        private SortedDictionary<int, (string, string, string, string)> MonsterTypeKillByNumber = new SortedDictionary<int, (string, string, string, string)>();
        // Map : [ UUID, Monster Type ]
        private Dictionary<string, List<(string, string)>> MonsterTypeKillByMap = new Dictionary<string, List<(string, string)>>();

        /// <summary>
        ///     Toggle between Scriptless and Scripted modes
        /// </summary>
        private bool isScriptlessMode = true;

        /// <summary>
        ///     Default constructor
        /// </summary>
        public Verification()
        {
            InitializeComponent();
            this.Text = "Verification Scriptless";
            DemopathList = new List<string>();
            this.mrtb.DragDrop += Verification_DragDrop;
            this.mrtb.DragEnter += Verification_DragEnter;
            this.mrtb.AllowDrop = true;
            AllowDrop = true;
            textBuffer = new ColoredTextBuffer(this.mrtb.ForeColor);
        }

        private void openDemosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var of = new OpenFileDialog
            {
                Filter = @"Demo files (.dem) | *.dem",
                Multiselect = true
            };

            if (of.ShowDialog() == DialogResult.OK)
            {
                Verify(of.FileNames);
            }
            else
            {
                mrtb.Text = @"No file selected/bad file selected!";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void demostartCommandToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DemopathList.ToArray().Length <= 32)
                Clipboard.SetText("startdemos " + DemopathList
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(x => int.Parse(Regex.Match(x + "0", @"\d+").Value))
                    .ToList()
                    .Aggregate((c, n) => c + " " + n));
            else
            {
                Clipboard.SetText(DemopathList
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(x => int.Parse(Regex.Match(x + "0", @"\d+").Value))
                    .Batch(32)
                    .Aggregate(string.Empty, (x, y) => x + ";startdemos " + (y.Aggregate((c, n) => c + " " + n)))
                    .Substring(1));
            }
            using (var ni = new NotifyIcon())
            {
                ni.Icon = SystemIcons.Exclamation;
                ni.Visible = true;
                ni.ShowBalloonTip(5000, "VolvoWrench", "Demo names copied to clipboard", ToolTipIcon.Info);
            }
        }

        /// <summary>
        ///     Get chapter short name from level name. HL1 only at the moment.
        /// </summary>
        /// <param name="map">Level to find chapter for</param>
        string MapToChapter(string map)
        {
            if (map.StartsWith("c0"))
                return "AM";

            else if (map == "c1a0" || map == "c1a0d" || map == "c1a0a" || map == "c1a0b" || map == "c1a0c" || map == "c1a0e" ||
                     map == "c1a1" || map == "c1a1a" || map == "c1a1f" || map == "c1a1b" || map == "c1a1c" || map == "c1a1d")
                return "UC";

            else if (map == "c1a2" || map == "c1a2a" || map == "c1a2b" || map == "c1a2c" || map == "c1a2d")
                return "OC";

            else if (map == "c1a3" || map == "c1a3a" || map == "c1a3b" || map == "c1a3c" || map == "c1a3d")
                return "WGH";

            else if (map == "c1a4" || map == "c1a4k" || map == "c1a4b" || map == "c1a4f" || map == "c1a4d" ||
                     map == "c1a4e" || map == "c1a4i" || map == "c1a4g" || map == "c1a4j")
                return "BP";

            else if (map == "c2a1" || map == "c2a1a" || map == "c2a1b")
                return "PU";

            else if (map == "c2a2" || map == "c2a2a" || map == "c2a2b1" || map == "c2a2b2" || map == "c2a2c" ||
                     map == "c2a2d" || map == "c2a2e" || map == "c2a2f" || map == "c2a2g" || map == "c2a2h")
                return "OAR";

            else if (map == "c2a3" || map == "c2a3a" || map == "c2a3b" || map == "c2a3c" || map == "c2a3d" || map == "c2a3e")
                return "APP";

            else if (map == "c2a4" || map == "c2a4a" || map == "c2a4b" || map == "c2a4c")
                return "RP";

            else if (map == "c2a4d" || map == "c2a4e" || map == "c2a4f" || map == "c2a4g")
                return "QE";

            else if (map == "c2a5" || map == "c2a5w" || map == "c2a5x" || map == "c2a5a" || map == "c2a5b" ||
                     map == "c2a5c" || map == "c2a5d" || map == "c2a5e" || map == "c2a5f" || map == "c2a5g")
                return "ST";

            else if (map == "c3a1" || map == "c3a1a" || map == "c3a1b")
                return "FAF";

            else if (map == "c3a2e" || map == "c3a2" || map == "c3a2a" || map == "c3a2b" || map == "c3a2c" ||
                     map == "c3a2d" || map == "c3a2f")
                return "LC";

            else if (map == "c4a1")
                return "XEN";

            else if (map == "c4a1" || map == "c4a2" || map == "c4a2a" || map == "c4a2b")
                return "GL";

            else if (map == "c4a1a" || map == "c4a1b" || map == "c4a1c" || map == "c4a1d" || map == "c4a1e" || map == "c4a1f")
                return "INT";

            else
                return "END";
        }

        /// <summary>
        ///     Get the demo number from the name of the demo (i.e. demo_100.dem => 100)
        /// </summary>
        /// <param name="demoName">Filename of the demo</param>
        private int demoNumberFromString(string demoName)
        {
            try
            {
                // Substring the number
                int numberStart = demoName.LastIndexOf('_')+1;
                int numberEnd = demoName.LastIndexOf(".");
                string numStr = demoName.Substring(numberStart, numberEnd - numberStart);
                return int.Parse(numStr);
            } catch (Exception e)
            {
                MessageBox.Show(
                    "Could not parse demo number from filename: " + demoName + "\n\n" + e.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return int.MinValue;
            }
        }

        /// <summary>
        ///     Verifies the kills in an HL100 run
        /// </summary>
        /// <param name="files">Demo files</param>
        /// <param name="textBuffer">Verification output log</param>
        public void VerifyHl100Kills(string[] files, ColoredTextBuffer textBuffer)
        {
            bool hasErrors = false;
            bool hasWarnings = false;
            const int totalKills = 944;

            Dictionary<string, Dictionary<string, int>> referenceMonsterCountsByMap = new Dictionary<string, Dictionary<string, int>>
            {
                { "c1a1", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 1},
                        {"monster_zombie", 2},
                    }
                },
                { "c1a0c", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 1},
                    }
                },
                { "c1a1a", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 1},
                        {"monster_zombie", 2},
                    }
                },
                { "c1a1f", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 5},
                        {"monster_zombie", 2},
                    }
                },
                { "c1a1b", new Dictionary<string, int>
                    {
                        {"monster_houndeye", 4},
                        {"monster_headcrab", 5},
                        {"monster_zombie", 3},
                        {"monster_alien_slave", 1},
                    }
                },
                { "c1a1c", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 11},
                        {"monster_houndeye", 1},
                        {"monster_bullchicken", 2},
                        {"monster_barnacle", 7},
                    }
                },
                { "c1a2", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 21},
                        {"monster_barnacle", 3},
                        {"monster_zombie", 1},
                        {"monster_miniturret", 1},
                    }
                },
                { "c1a2d", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 2},
                        {"monster_zombie", 1},
                    }
                },
                { "c1a2a", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 10},
                        {"monster_alien_slave", 11},
                        {"monster_barnacle", 2},
                        {"monster_miniturret", 1},
                        {"monster_zombie", 1},
                    }
                },
                { "c1a2b", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 13},
                        {"monster_bullchicken", 1},
                        {"monster_alien_slave", 3},
                        {"monster_zombie", 4},
                    }
                },
                { "c1a2c", new Dictionary<string, int>
                    {
                        {"monster_zombie", 2},
                        {"monster_bullchicken", 2},
                        {"monster_headcrab", 7},
                        {"monster_barnacle", 4},
                    }
                },
                { "c1a3", new Dictionary<string, int>
                    {
                        {"monster_zombie", 1},
                        {"monster_headcrab", 4},
                        {"monster_sentry", 5},
                        {"monster_alien_slave", 2},
                        {"monster_human_grunt", 2},
                    }
                },
                { "c1a3d", new Dictionary<string, int>
                    {
                        {"monster_sentry", 2},
                        {"monster_human_grunt", 1},
                    }
                },
                { "c1a3a", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 10},
                        {"monster_barnacle", 11},
                        {"monster_sentry", 3},
                    }
                },
                { "c1a3b", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 4},
                        {"monster_osprey", 1},
                    }
                },
                { "c1a3c", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 2},
                        {"monster_osprey", 1},
                    }
                },
                { "c1a4", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 7},
                        {"monster_bullchicken", 5},
                        {"monster_zombie", 1},
                        {"monster_houndeye", 5},
                    }
                },
                { "c1a4k", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 4},
                    }
                },
                { "c1a4b", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 2},
                        {"monster_houndeye", 6},
                        {"monster_headcrab", 2},
                        {"monster_zombie", 1},
                    }
                },
                { "c1a4i", new Dictionary<string, int>
                    {
                        {"monster_zombie", 2},
                        {"monster_barnacle", 2},
                        {"monster_tentacle", 3},
                    }
                },
                { "c1a4f", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 2},
                        {"monster_houndeye", 3},
                        {"monster_barnacle", 3},
                        {"monster_zombie", 1},
                    }
                },
                { "c1a4d", new Dictionary<string, int>
                    {
                        {"monster_zombie", 6},
                        {"monster_bullchicken", 2},
                        {"monster_headcrab", 1},
                    }
                },
                { "c1a4e", new Dictionary<string, int>
                    {
                        {"monster_zombie", 2},
                        {"monster_headcrab", 3},
                    }
                },
                { "c1a4j", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 1},
                    }
                },
                { "c2a1", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 2},
                        {"monster_alien_slave", 7},
                        {"monster_headcrab", 9},
                        {"monster_gargantua", 1},
                    }
                },
                { "c2a1b", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 2},
                        {"monster_bullchicken", 1},
                        {"monster_human_grunt", 3},
                        {"monster_sentry", 1},
                    }
                },
                { "c2a1a", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 14},
                        {"monster_headcrab", 3},
                        {"monster_houndeye", 5},
                        {"monster_zombie", 1},
                    }
                },
                { "c2a2", new Dictionary<string, int>
                    {
                        {"monster_barnacle", 4},
                    }
                },
                { "c2a2a", new Dictionary<string, int>
                    {
                        {"monster_houndeye", 5},
                        {"monster_headcrab", 2},
                        {"monster_bullchicken", 3},
                        {"monster_sentry", 1},
                        {"monster_barnacle", 4},
                    }
                },
                { "c2a2b2", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 4},
                        {"monster_human_grunt", 1},
                        {"monster_headcrab", 7},
                        {"monster_houndeye", 3},
                        {"monster_barnacle", 3},
                    }
                },
                { "c2a2b1", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 11},
                        {"monster_headcrab", 2},
                        {"monster_alien_slave", 4},
                    }
                },
                { "c2a2c", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 8},
                        {"monster_human_grunt", 5},
                        {"monster_headcrab", 1},
                    }
                },
                { "c2a2d", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 7},
                        {"monster_alien_slave", 4},
                        {"monster_headcrab", 6},
                        {"monster_bullchicken", 3},
                        {"monster_sentry", 3},
                        {"monster_barnacle", 1},
                    }
                },
                { "c2a2e", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 11},
                        {"monster_alien_slave", 6},
                        {"monster_headcrab", 2},
                    }
                },
                { "c2a2f", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 3},
                        {"monster_sentry", 2},
                    }
                },
                { "c2a2g", new Dictionary<string, int>
                    {
                        {"monster_sentry", 4},
                        {"monster_zombie", 2},
                        {"monster_human_grunt", 4},
                        {"func_breakable", 1},
                    }
                },
                { "c2a2h", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 5},
                    }
                },
                { "c2a3", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 3},
                        {"monster_zombie", 2},
                    }
                },
                { "c2a3a", new Dictionary<string, int>
                    {
                        {"monster_ichthyosaur", 1},
                        {"monster_barnacle", 7},
                    }
                },
                { "c2a3b", new Dictionary<string, int>
                    {
                        {"monster_barnacle", 6},
                        {"monster_alien_slave", 3},
                        {"monster_ichthyosaur", 2},
                        {"monster_bullchicken", 3},
                        {"monster_headcrab", 1},
                    }
                },
                { "c2a3c", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 6},
                        {"monster_headcrab", 6},
                    }
                },
                { "c2a3d", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 4},
                        {"monster_human_assassin", 3},
                    }
                },
                { "c2a4", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 2},
                    }
                },
                { "c2a4a", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 4},
                        {"monster_barnacle", 4},
                    }
                },
                { "c2a4b", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 4},
                    }
                },
                { "c2a4c", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 4},
                        {"monster_bullchicken", 1},
                        {"monster_barnacle", 6},
                    }
                },
                { "c2a4d", new Dictionary<string, int>
                    {
                        {"monster_houndeye", 5},
                        {"monster_alien_grunt", 1},
                        {"monster_headcrab", 5},
                        {"monster_human_grunt", 1},
                    }
                },
                { "c2a4e", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 11},
                        {"monster_headcrab", 14},
                        {"monster_alien_grunt", 2},
                    }
                },
                { "c2a4f", new Dictionary<string, int>
                    {
                        {"monster_bullchicken", 3},
                        {"monster_human_grunt", 4},
                        {"monster_houndeye", 4},
                    }
                },
                { "c2a4g", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 1},
                        {"monster_sentry", 2},
                    }
                },
                { "c2a5", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 5},
                        {"monster_alien_slave", 1},
                        {"monster_apache", 1},
                        {"func_breakable", 1},
                        {"monster_ichthyosaur", 1},
                    }
                },
                { "c2a5w", new Dictionary<string, int>
                    {
                        {"monster_apache", 1},
                        {"monster_headcrab", 3},
                        {"monster_houndeye", 1},
                        {"monster_human_grunt", 4},
                    }
                },
                { "c2a5x", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 6},
                    }
                },
                { "c2a5a", new Dictionary<string, int>
                    {
                        {"monster_sentry", 1},
                        {"monster_human_grunt", 5},
                        {"monster_apache", 1},
                        {"monster_headcrab", 1},
                    }
                },
                { "c2a5b", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 9},
                        {"func_breakable", 2},
                    }
                },
                { "c2a5c", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 1},
                        {"monster_alien_slave", 1},
                        {"func_breakable", 2},
                    }
                },
                { "c2a5d", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 3},
                    }
                },
                { "c2a5e", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 12},
                        {"monster_sentry", 1},
                        {"func_breakable", 2},
                        {"monster_alien_grunt", 8},
                        {"monster_osprey", 1},
                    }
                },
                { "c2a5f", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 21},
                        {"monster_alien_slave", 11},
                        {"monster_alien_grunt", 10},
                        {"monster_headcrab", 5},
                    }
                },
                { "c2a5g", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 2},
                        {"monster_gargantua", 1},
                    }
                },
                { "c3a1", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 7},
                        {"monster_alien_slave", 5},
                        {"monster_turret", 1},
                        {"monster_barnacle", 1},
                    }
                },
                { "c3a1a", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 5},
                        {"monster_sentry", 3},
                        {"monster_barnacle", 4},
                        {"monster_ichthyosaur", 1},
                        {"monster_human_grunt", 3},
                        {"func_breakable", 1},
                    }
                },
                { "c3a1b", new Dictionary<string, int>
                    {
                        {"monster_human_grunt", 5},
                        {"monster_alien_grunt", 7},
                        {"monster_alien_slave", 3},
                        {"func_breakable", 1},
                    }
                },
                { "c3a2e", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 6},
                        {"monster_bullchicken", 1},
                        {"monster_human_assassin", 4},
                    }
                },
                { "c3a2", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 4},
                        {"monster_headcrab", 6},
                        {"monster_bullchicken", 1},
                    }
                },
                { "c3a2a", new Dictionary<string, int>
                    {
                        {"monster_barnacle", 6},
                        {"monster_alien_grunt", 11},
                        {"monster_headcrab", 5},
                        {"monster_alien_slave", 7},
                    }
                },
                { "c3a2b", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 4},
                    }
                },
                { "c3a2c", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 5},
                        {"monster_alien_slave", 2},
                        {"monster_headcrab", 5},
                    }
                },
                { "c3a2d", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 3},
                        {"monster_alien_controller", 3},
                    }
                },
                { "c4a1", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 2},
                        {"monster_houndeye", 5},
                        {"func_breakable", 4},
                    }
                },
                { "c4a2", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 2},
                    }
                },
                { "c4a2a", new Dictionary<string, int>
                    {
                        {"monster_headcrab", 3},
                    }
                },
                { "c4a2b", new Dictionary<string, int>
                    {
                        {"monster_bigmomma", 1},
                    }
                },
                { "c4a1a", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 6},
                        {"monster_alien_controller", 5},
                        {"monster_headcrab", 2},
                        {"monster_barnacle", 3},
                        {"monster_bullchicken", 1},
                    }
                },
                { "c4a1b", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 6},
                        {"monster_alien_slave", 5},
                        {"monster_alien_controller", 3},
                        {"monster_gargantua", 1},
                        {"monster_barnacle", 3},
                    }
                },
                { "c4a1c", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 6},
                        {"monster_alien_controller", 2},
                    }
                },
                { "c4a1d", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 18},
                        {"monster_alien_controller", 9},
                        {"monster_alien_grunt", 12},
                    }
                },
                { "c4a1e", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 13},
                        {"monster_alien_controller", 9},
                        {"monster_alien_grunt", 8},
                    }
                },
                { "c4a3", new Dictionary<string, int>
                    {
                        {"monster_alien_controller", 8},
                        {"monster_alien_slave", 3},
                        {"monster_ichthyosaur", 1},
                        {"monster_gargantua", 1},
                        {"monster_nihilanth", 1},
                    }
                },
            };

            Dictionary<string, Dictionary<string, int>> referenceMonsterCountsByChapter = new Dictionary<string, Dictionary<string, int>>
            {
                { "UC", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 1},
                        {"monster_barnacle", 7},
                        {"monster_bullchicken", 2},
                        {"monster_headcrab", 24},
                        {"monster_houndeye", 5},
                        {"monster_zombie", 9},
                    }
                },
                { "OC", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 14},
                        {"monster_barnacle", 9},
                        {"monster_bullchicken", 3},
                        {"monster_headcrab", 53},
                        {"monster_miniturret", 2},
                        {"monster_zombie", 9},
                    }
                },
                { "WGH", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 2},
                        {"monster_barnacle", 11},
                        {"monster_headcrab", 4},
                        {"monster_human_grunt", 19},
                        {"monster_osprey", 2},
                        {"monster_sentry", 10},
                        {"monster_zombie", 1},
                    }
                },
                { "BP", new Dictionary<string, int>
                    {
                        {"monster_barnacle", 5},
                        {"monster_bullchicken", 15},
                        {"monster_headcrab", 14},
                        {"monster_houndeye", 14},
                        {"monster_tentacle", 3},
                        {"monster_zombie", 13},
                    }
                },
                { "PU", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 9},
                        {"monster_bullchicken", 1},
                        {"monster_gargantua", 1},
                        {"monster_headcrab", 12},
                        {"monster_houndeye", 5},
                        {"monster_human_grunt", 19},
                        {"monster_sentry", 1},
                        {"monster_zombie", 1},
                    }
                },
                { "OAR", new Dictionary<string, int>
                    {
                        {"func_breakable", 1},
                        {"monster_alien_slave", 22},
                        {"monster_barnacle", 12},
                        {"monster_bullchicken", 10},
                        {"monster_headcrab", 20},
                        {"monster_houndeye", 8},
                        {"monster_human_grunt", 47},
                        {"monster_sentry", 10},
                        {"monster_zombie", 2},
                    }
                },
                { "APP", new Dictionary<string, int>
                    {
                        {"monster_alien_slave", 13},
                        {"monster_barnacle", 13},
                        {"monster_bullchicken", 3},
                        {"monster_headcrab", 7},
                        {"monster_human_assassin", 3},
                        {"monster_ichthyosaur", 3},
                        {"monster_zombie", 2},
                    }
                },
                { "RP", new Dictionary<string, int>
                    {
                        {"monster_barnacle", 10},
                        {"monster_bullchicken", 5},
                        {"monster_headcrab", 10},
                    }
                },
                { "QE", new Dictionary<string, int>
                    {
                        {"monster_alien_grunt", 3},
                        {"monster_headcrab", 19},
                        {"monster_houndeye", 9},
                        {"monster_human_grunt", 17},
                        {"monster_sentry", 2},
                    }
                },
                { "ST", new Dictionary<string, int>
                    {
                        {"func_breakable", 7},
                        {"monster_alien_grunt", 19},
                        {"monster_alien_slave", 13},
                        {"monster_apache", 3},
                        {"monster_gargantua", 1},
                        {"monster_headcrab", 18},
                        {"monster_houndeye", 1},
                        {"monster_human_grunt", 58},
                        {"monster_ichthyosaur", 1},
                        {"monster_osprey", 1},
                        {"monster_sentry", 2},
                    }
                },
                { "FAF", new Dictionary<string, int>
                    {
                        {"func_breakable", 2},
                        {"monster_alien_grunt", 14},
                        {"monster_alien_slave", 8},
                        {"monster_barnacle", 5},
                        {"monster_headcrab", 5},
                        {"monster_human_grunt", 8},
                        {"monster_ichthyosaur", 1},
                        {"monster_sentry", 3},
                        {"monster_turret", 1},
                    }
                },
                { "LC", new Dictionary<string, int>
                    {
                        {"monster_alien_controller", 3},
                        {"monster_alien_grunt", 20},
                        {"monster_alien_slave", 13},
                        {"monster_barnacle", 6},
                        {"monster_bullchicken", 2},
                        {"monster_headcrab", 25},
                    }
                },
                { "XEN", new Dictionary<string, int>
                    {
                        {"func_breakable", 4},
                        {"monster_alien_slave", 2},
                        {"monster_houndeye", 5},
                    }
                },
                { "GL", new Dictionary<string, int>
                    {
                        {"monster_bigmomma", 1},
                        {"monster_headcrab", 5},
                    }
                },
                { "INT", new Dictionary<string, int>
                    {
                        {"monster_alien_controller", 28},
                        {"monster_alien_grunt", 26},
                        {"monster_alien_slave", 48},
                        {"monster_barnacle", 6},
                        {"monster_bullchicken", 1},
                        {"monster_gargantua", 1},
                        {"monster_headcrab", 2},
                    }
                },
                { "END", new Dictionary<string, int>
                    {
                        {"monster_alien_controller", 8},
                        {"monster_alien_slave", 3},
                        {"monster_gargantua", 1},
                        {"monster_ichthyosaur", 1},
                        {"monster_nihilanth", 1},
                    }
                },
            };

            Dictionary<string, Dictionary<string, int>> monsterCountsByMap = new Dictionary<string, Dictionary<string, int>>();

            // Build dict of kills of by map and monster type
            foreach (var map in MonsterTypeKillByMap)
            {
                foreach (var kill in MonsterTypeKillByMap[map.Key])
                {
                    if (!monsterCountsByMap.ContainsKey(map.Key))
                    {
                        monsterCountsByMap.Add(map.Key, new Dictionary<string, int>());
                    }
                    if (!monsterCountsByMap[map.Key].ContainsKey(kill.Item2))
                    {
                        monsterCountsByMap[map.Key].Add(kill.Item2, 0);
                    }
                    monsterCountsByMap[map.Key][kill.Item2]++;
                }
            }

            List<(string, Color)> killsByCountLines = new List<(string, Color)>();
            List<(string, Color)> killsByMapLines = new List<(string, Color)>();
            List<(string, Color)> killsByChapterLines = new List<(string, Color)>();

            textBuffer.Append("HL100 Kill table:\n");
            // Dump the kills by map to the output so it can be cross referenced with the spreadsheet if needed.
            foreach (var map in referenceMonsterCountsByMap)
            {
                killsByMapLines.Add(($"Kills on {map.Key}:", DefaultColor));
                foreach (var kill in referenceMonsterCountsByMap[map.Key])
                {
                    int actualKills = 0;
                    if (monsterCountsByMap.ContainsKey(map.Key) && monsterCountsByMap[map.Key].ContainsKey(kill.Key))
                    {
                        actualKills = monsterCountsByMap[map.Key][kill.Key];
                    }
                    Color c = DefaultColor;
                    if (actualKills != kill.Value)
                    {
                        c = WarningColor;
                    }
                    killsByMapLines.Add(($"  {kill.Key}: {actualKills} (Expected: {kill.Value})", c));
                }
                killsByMapLines.Add(("", DefaultColor));
            }

            var monsterCountsByChapter = new Dictionary<string, Dictionary<string, int>>();

            foreach (var kv in monsterCountsByMap)
            {
                var mapName = kv.Key;
                var monsters = kv.Value;
                var chapter = MapToChapter(mapName);

                if (!monsterCountsByChapter.TryGetValue(chapter, out var chapterDict))
                {
                    chapterDict = new Dictionary<string, int>();
                    monsterCountsByChapter[chapter] = chapterDict;
                }

                foreach (var m in monsters)
                {
                    if (chapterDict.ContainsKey(m.Key))
                        chapterDict[m.Key] += m.Value;
                    else
                        chapterDict[m.Key] = m.Value;
                }
            }

            // Dump monsters by chapter to the textBuffer
            foreach (var chapter in referenceMonsterCountsByChapter)
            {
                killsByChapterLines.Add(($"Kills on {chapter.Key}:", DefaultColor));
                foreach (var m in chapter.Value)
                {
                    int actualKills = 0;
                    if (monsterCountsByChapter.ContainsKey(chapter.Key) && monsterCountsByChapter[chapter.Key].ContainsKey(m.Key))
                    {
                        actualKills = monsterCountsByChapter[chapter.Key][m.Key];
                    }
                    Color c = DefaultColor;
                    if (actualKills != m.Value)
                    {
                        c = WarningColor;
                    }
                    killsByChapterLines.Add(($"  {m.Key}: {actualKills} (Expected: {m.Value})", c));
                }
                killsByChapterLines.Add(("", DefaultColor));
                killsByChapterLines.Add(("", DefaultColor));
            }

            // Dump the kills by number to the textBuffer so it can be cross referenced with the spreadsheet if needed.
            killsByCountLines.Add(("Kills by number:", DefaultColor));
            foreach (var kill in MonsterTypeKillByNumber)
            {
                if (!string.IsNullOrEmpty(kill.Value.Item3))
                {
                    killsByCountLines.Add(($"  {kill.Key}: {kill.Value.Item2} '{kill.Value.Item3}' killed on {kill.Value.Item4}", DefaultColor));
                }
                else
                {
                    killsByCountLines.Add(($"  {kill.Key}: {kill.Value.Item2} killed on {kill.Value.Item4}", DefaultColor));
                }
            }

            // Format as a more easily-readable table
            for (int i = 0; i < Math.Max(killsByChapterLines.Count(), Math.Max(killsByMapLines.Count(), killsByCountLines.Count())); i++)
            {
                
                if (i < killsByCountLines.Count())
                {
                    textBuffer.Append(killsByCountLines[i].Item1.PadRight(70), killsByCountLines[i].Item2);
                }
                else
                {
                    textBuffer.Append("".PadRight(70));
                }

                textBuffer.Append(" | ");

                if (i < killsByMapLines.Count())
                {
                    textBuffer.Append(killsByMapLines[i].Item1.PadRight(50), killsByMapLines[i].Item2);
                }
                else
                {
                    textBuffer.Append("".PadRight(50));
                }

                textBuffer.Append(" | ");

                if (i < killsByChapterLines.Count())
                {
                    textBuffer.Append(killsByChapterLines[i].Item1.PadRight(50), killsByChapterLines[i].Item2);
                }
                else
                {
                    textBuffer.Append("".PadRight(50));
                }

                textBuffer.Append("\n");
            }
            textBuffer.Append("\n");

            // Verify they got exactly totalKills kills
            for (int i = 1; i <= totalKills; i++)
            {
                if (!MonsterTypeKillByNumber.ContainsKey(i))
                {
                    string err = "HL100: Missing kill #" + i + "\n";
                    Df[files.Last()].GsDemoInfo.ParsingErrors.Add(err);
                    textBuffer.Append(err, IllegalColor);
                    hasErrors = true;
                }
            }

            // Verify there weren't any illegally bound report_to_demo commands
            foreach (var df in Df)
            {
                foreach (var cheat in df.Value.GsDemoInfo.Cheats)
                {
                    if (cheat.ToUpper().Contains("REPORT_TO_DEMO"))
                    {
                        hasErrors = true;
                    }
                }
            }

            // Check for extra kills
            if (MonsterTypeKillByNumber.Count() > totalKills)
            {
                // Add error to last demo for extra kills
                string err = "HL100: Extra kills in run! There should only be " + totalKills + ".\n";
                Df[files.Last()].GsDemoInfo.ParsingErrors.Add(err);
                textBuffer.Append(err, IllegalColor);
                hasErrors = true;
            }

            // Verify the last kill is Nihilanth
            if (!MonsterTypeKillByNumber.ContainsKey(totalKills) || MonsterTypeKillByNumber[totalKills].Item2 != "monster_nihilanth")
            {
                // Add error to last demo for missing kill
                string err = "HL100: Last kill is not nihilanth or there is no " + totalKills + "th kill!\n";
                Df[files.Last()].GsDemoInfo.ParsingErrors.Add(err);
                textBuffer.Append(err, IllegalColor);
                hasErrors = true;
            }
            else
            {
                textBuffer.Append("\nHL100: Got 944 kills with Nihilanth as the final kill.\n\n", Color.Green);
            }

            // Verify expected kills and check for count mismatches
            foreach (var map in referenceMonsterCountsByMap)
            {
                foreach (var monsterTypeKills in referenceMonsterCountsByMap[map.Key])
                {
                    string expectedMonster = monsterTypeKills.Key;
                    int expectedCount = monsterTypeKills.Value;
                    string mapName = map.Key;

                    if (!monsterCountsByMap.ContainsKey(mapName) || !monsterCountsByMap[mapName].ContainsKey(expectedMonster))
                    {
                        textBuffer.Append(
                            $"HL100 Map:{mapName,-6} | SHORT | {expectedMonster} | Expected: {expectedCount,-2} | Got: 0\n",
                            WarningColor);
                        hasWarnings = true;
                    }
                    else
                    {
                        int actualCount = monsterCountsByMap[mapName][expectedMonster];
                        if (actualCount < expectedCount)
                        {
                            textBuffer.Append(
                                $"HL100 Map:{mapName,-6} | SHORT | {expectedMonster} | Expected: {expectedCount,-2} | Got: {actualCount,-2} ({expectedCount - actualCount} short)\n",
                                WarningColor);
                            hasWarnings = true;
                        }
                        else if (actualCount > expectedCount)
                        {
                            textBuffer.Append(
                                $"HL100 Map:{mapName,-6} | EXTRA | {expectedMonster} | Expected: {expectedCount,-2} | Got: {actualCount,-2} ({actualCount - expectedCount} extra)\n",
                                WarningColor);
                            hasWarnings = true;
                        }
                    }
                }
            }

            // Find unexpected extra monsters/maps
            foreach (var actualMap in monsterCountsByMap)
            {
                string actualMapName = actualMap.Key;
                foreach (var actualKills in actualMap.Value)
                {
                    string actualMonster = actualKills.Key;
                    int actualCount = actualKills.Value;

                    if (!referenceMonsterCountsByMap.ContainsKey(actualMapName) || 
                        !referenceMonsterCountsByMap[actualMapName].ContainsKey(actualMonster))
                    {
                        textBuffer.Append(
                            $"HL100 Map:{actualMapName,-6} | STRAY | {actualMonster} | Expected: 0  | Got: {actualCount,-2} (unexpected)\n",
                            WarningColor);
                        hasWarnings = true;
                    }
                }
            }

            if (hasErrors)
            {
                textBuffer.Append("\nHL100: Verification did not pass.\n", IllegalColor);
            }
            else if (hasWarnings)
            {
                textBuffer.Append("\nHL100: Verification passed with warnings. " +
                    "Note that kills may happen on unexpected maps due to crossing level transitions.\n",
                    WarningColor);
            }
            else
            {
                textBuffer.Append("\nHL100: Verification passed.\n", GoodColor);
            }
        }

        /// <summary>
        ///     This is the actuall verification method
        /// </summary>
        /// <param name="files">The paths of the files</param>
        public void Verify(string[] files)
        {
            files = files.OrderBy(f => demoNumberFromString(f)).ToArray(); // HL100 needs the demos to be parsed in order
            Df.Clear();
            MonsterTypeKillByNumber.Clear();
            MonsterTypeKillByMap.Clear();
            mrtb.Font = new Font("Consolas", 12, FontStyle.Regular); // Need a monospaced font for table output
            mrtb.WordWrap = false;
            mrtb.Text = $@"Please wait. Parsing demos... 0/{files.Length}";
            var curr = 0;
            foreach (var dt in files.Where(file => File.Exists(file) && Path.GetExtension(file) == ".dem"))
            {
                DemopathList.Add(dt);
                Df.Add(dt, CrossDemoParser.Parse(dt)); //If someone bothers me that its slow make it async.
                mrtb.Text = $@"Please wait. Parsing demos... {curr++}/{files.Length}";
            }
            if (Df.Any(x => x.Value.GsDemoInfo.ParsingErrors.Count > 0))
            {
                var brokendemos = Df.Where(x => x.Value.GsDemoInfo.ParsingErrors.Count > 0)
                    .ToList()
                    .Aggregate("", (c, n) => c += "\n" + n.Key);
                MessageBox.Show(@"Broken demos found:
" + brokendemos, @"Error!", MessageBoxButtons.OK);
                Main.Log("Broken demos when verification: " + brokendemos);
                mrtb.Text = @"Please fix the demos then reselect the files!";
                return;
            }
            if (Df.Any(x => x.Value.Type != Parseresult.GoldSource))
                MessageBox.Show(@"Only goldsource supported");
            else
            {
                mrtb.Text = "";
                textBuffer.Append("" + "\n");
                textBuffer.Append("Parsed demos. Results:" + "\n");
                textBuffer.Append("General stats:" + "\n");
                textBuffer.Append($@"
Highest FPS:                {(1/Df.Select(x => x.Value).ToList().Min(y => y.GsDemoInfo.AditionalStats.FrametimeMin)).ToString("N2")}
Lowest FPS:                 {(1/Df.Select(x => x.Value).ToList().Max(y => y.GsDemoInfo.AditionalStats.FrametimeMax)).ToString("N2")}
Average FPS:                {(Df.Select(z => z.Value).ToList().Average(k => k.GsDemoInfo.AditionalStats.Count/k.GsDemoInfo.AditionalStats.FrametimeSum)).ToString("N2")}
Lowest msec:                {(1000.0/Df.Select(x => x.Value).ToList().Min(y => y.GsDemoInfo.AditionalStats.MsecMin)).ToString("N2")} FPS
Highest msec:               {(1000.0/Df.Select(x => x.Value).ToList().Max(y => y.GsDemoInfo.AditionalStats.MsecMax)).ToString("N2")} FPS
Average msec:               {(Df.Select(x => x.Value).ToList().Average(y => y.GsDemoInfo.AditionalStats.MsecSum/(double) y.GsDemoInfo.AditionalStats.Count)).ToString("N2")} FPS

Total time of the demos:    {Df.Sum(x => x.Value.GsDemoInfo.DirectoryEntries.Sum(y => y.TrackTime))}s
Human readable time:        {TimeSpan.FromSeconds(Df.Sum(x => x.Value.GsDemoInfo.DirectoryEntries.Sum(y => y.TrackTime))).ToString("g")}" + "\n\n");

                textBuffer.Append("Demo cheat check:" + "\n");
                BXTTreeView.BeginUpdate();  //prevent UI tree updates during processing
                var cur = 0;
                mrtb.Text = $@"Please wait. Analyzing demos... 0/{files.Length}";
                foreach (var dem in Df)
                {
                    if (dem.Value.GsDemoInfo.Cheats.Count > 0)
                    {
                        textBuffer.Append("Possible cheats:\n");
                        foreach (var cheat in dem.Value.GsDemoInfo.Cheats.Distinct())
                        {
                            textBuffer.Append("\t" + cheat + "\n");
                        }
                    }
                    textBuffer.Append(Path.GetFileName(dem.Key) + " -> " + dem.Value.GsDemoInfo.Header.MapName);
                    textBuffer.Append("\nBXTData:\n");
                    ParseBxtData(dem);
                    textBuffer.Append("\n");
                    mrtb.Text = $@"Please wait. Analyzing demos... {cur++}/{files.Length}";
                    Application.DoEvents();
                }
                if (MonsterTypeKillByNumber.Count > 0)
                {
                    VerifyHl100Kills(files, textBuffer);
                }
                mrtb.Clear();
                textBuffer.AppendToRichTextBox(mrtb);
                textBuffer.Clear();
                BXTTreeView.EndUpdate();    //draw treeview after processing
            }
        }

        /// <summary>
        /// Parses the bxt data into treenodes
        /// </summary>
        /// <param name="Infos"></param>
        public void ParseBxtData(KeyValuePair<string, CrossParseResult> info)
        {
            const string bxtVersion = "cbc496b1ba7f6c242a961c33f16d3b5741371dd6-CLEAN based on nov-11-2024";
            var cvarRules = new Dictionary<string, string>()
            {
                {"_BXT_MIN_FRAMETIME", "0"},
                {"_BXT_NOREFRESH", "0"},
                {"_BXT_SAVE_RUNTIME_DATA_IN_DEMOS", "1"},
                {"_BXT_TASLOG", "0"},
                {"BGMBUFFER", "4096"},
                {"BXT_BHOPCAP", "0"},
                {"BXT_COLLISION_DEPTH_MAP", "0"},
                {"BXT_DISABLE_BRUSH_ENTITIES", "0"},
                {"BXT_DISABLE_CHANGELEVEL", "0"},
                {"BXT_DISABLE_HUD", "0"},
                {"BXT_DISABLE_PARTICLES", "0"},
                {"BXT_DISABLE_PLAYER_CORPSES", "0"},
                {"BXT_DISABLE_SPRITE_ENTITIES", "0"},
                {"BXT_DISABLE_STUDIO_ENTITIES", "0"},
                {"BXT_DISABLE_VGUI", "0"},
                {"BXT_DISABLE_WORLD", "0"},
                {"BXT_FADE_REMOVE", "0"},
                {"BXT_FIRE_ON_BUTTON_COMMAND", ""},
                {"BXT_FIRE_ON_BUTTON_TARGET", ""},
                {"BXT_FIRE_ON_MM_COMMAND", ""},
                {"BXT_FIRE_ON_MM_TARGETNAME", ""},
                {"BXT_FIRE_ON_STUCK", ""},
                {"BXT_FORCE_DUCK", "0"},
                {"BXT_FORCE_JUMPLESS", "0"},
                {"BXT_FORCE_ZMAX", "0"},
                {"BXT_HIDE_OTHER_PLAYERS", "0"},
                {"BXT_HUD_ARMOR", "0"},
                {"BXT_HUD_CHECKPOINT", "0"},
                {"BXT_HUD_DISTANCE", "0"},
                {"BXT_HUD_ENTITIES", "0"},
                {"BXT_HUD_ENTITY_HP", "0"},
                {"BXT_HUD_ENTITY_INFO", "0"},
                {"BXT_HUD_GONARCH", "0"},
                {"BXT_HUD_HEALTH", "0"},
                {"BXT_HUD_NIHILANTH", "0"},
                {"BXT_HUD_ORIGIN", "0"},
                {"BXT_HUD_QUICKGAUSS", "0"},
                {"BXT_HUD_SCALE", "0"},
                {"BXT_HUD_SELFGAUSS", "0"},
                {"BXT_HUD_STAMINA", "0"},
                {"BXT_HUD_TAS_EDITOR_STATUS", "0"},
                {"BXT_HUD_USEABLES", "0"},
                {"BXT_HUD_VELOCITY", "0"},
                {"BXT_HUD_VISIBLE_LANDMARKS", "0"},
                {"BXT_HUD_WATERLEVEL", "0"},
                {"BXT_INTERPROCESS_ENABLE", "0"},
                {"BXT_LIGHTSTYLE", "0"},
                {"BXT_LIGHTSTYLE_CUSTOM", ""},
                {"BXT_NOVIS", "0"},
                {"BXT_REMOVE_FPS_LIMIT", "0"},
                {"BXT_REMOVE_PUNCHANGLES", "0"},
                {"BXT_RENDER_FAR_ENTITIES", "0"},
                {"BXT_SHAKE_REMOVE", "0"},
                {"BXT_SHOW_BULLETS", "0"},
                {"BXT_SHOW_BULLETS_ENEMY", "0"},
                {"BXT_SHOW_CINE_MONSTERS", "0"},
                {"BXT_SHOW_CUSTOM_TRIGGERS", "1"},
                {"BXT_SHOW_DISPLACER_EARTH_TARGETS", "0"},
                {"BXT_SHOW_HIDDEN_ENTITIES", "0"},
                {"BXT_SHOW_HIDDEN_ENTITIES_CLIENTSIDE", "0"},
                {"BXT_SHOW_MONSTER_BBOX", "0"},
                {"BXT_SHOW_NODES", "0"},
                {"BXT_SHOW_ONLY_PLAYERS", "0"},
                {"BXT_SHOW_PICKUP_BBOX", "0"},
                {"BXT_SHOW_PLAYER_BBOX", "0"},
                {"BXT_SHOW_PLAYER_IN_HLTV", "0"},
                {"BXT_SHOW_ROUTES", "0"},
                {"BXT_SHOW_SOUNDS", "0"},
                {"BXT_SHOW_SPLITS", "0"},
                {"BXT_SHOW_TRIGGERS", "0"},
                {"BXT_SHOW_TRIGGERS_LEGACY", "0"},
                {"BXT_SKYBOX_NAME", ""},
                {"BXT_SKYBOX_REMOVE", "0"},
                {"BXT_SPLITS_END_ON_LAST_SPLIT", "0"},
                {"BXT_SPLITS_START_TIMER_ON_FIRST_SPLIT", "0"},
                {"BXT_TAS_EDITOR_CAMERA_EDITOR", "0"},
                {"BXT_TAS_EDITOR_SIMULATE_FOR_MS", "40"},
                {"BXT_TAS_NOREFRESH_UNTIL_LAST_FRAMES", "0"},
                {"BXT_TAS_PLAYBACK_SPEED", "1"},
                {"BXT_TAS_WRITE_LOG", "0"},
                {"BXT_UNLOCK_CAMERA_DURING_PAUSE", "0"},
                {"BXT_WALLHACK", "0"},
                {"BXT_WATER_REMOVE", "0"},
                {"C_MAXDISTANCE", "200.0"},
                {"C_MAXPITCH", "90.0"},
                {"C_MAXYAW", "135.0"},
                {"C_MINDISTANCE", "30.0"},
                {"C_MINPITCH", "0.0"},
                {"C_MINYAW", "-135.0"},
                {"CAM_COMMAND", "0"},
                {"CAM_CONTAIN", "0"},
                {"CAM_IDEALDIST", "64"},
                {"CAM_IDEALPITCH", "0"},
                {"CAM_IDEALYAW", "90"},
                {"CAM_SNAPTO", "0"},
                {"CHASE_ACTIVE", "0"},
                {"CHASE_BACK", "100"},
                {"CHASE_RIGHT", "0"},
                {"CHASE_UP", "16"},
                {"CL_ANGLESPEEDKEY", "0.67"},
                {"CL_BACKSPEED", "400"},
                {"CL_BHOP_MODE", "ほぼ"},
                {"CL_CLOCKRESET", "0.1"},
                {"CL_CMDBACKUP", "2"},
                {"CL_FIXTIMERATE", "7.5"},
                {"CL_FORWARDSPEED", "400"},
                {"CL_GAITESTIMATION", "1"},
                {"CL_GG", "0"},
                {"CL_IDEALPITCHSCALE", "0.8"},
                {"CL_LC", "1"},
                {"CL_MOVESPEEDKEY", "0.3"},
                {"CL_NEEDINSTANCED", "0"},
                {"CL_NOSMOOTH", "0"},
                {"CL_PITCHSPEED", "225"},
                {"CL_RULER_ENABLE", "ほぼ"},
                {"CL_SHOWNET", "0"},
                {"CL_SIDESPEED", "400"},
                {"CL_SLIST", "10.0"},
                {"CL_SMOOTHTIME", "0.1"},
                {"CL_SOLID_PLAYERS", "1"},
                {"CL_UPSPEED", "320"},
                {"CL_VSMOOTHING", "0.05"},
                {"CL_WATERDIST", "4"},
                {"CL_YAWSPEED", "210"},
                {"CLIENTPORT", "27005"},
                {"COM_FILEWARNING", "0"},
                {"COOP", "0"},
                {"D_SPRITESKIP", "0"},
                {"DEATHMATCH", "0"},
                {"DEV_OVERVIEW", "0"},
                {"DIRECT", "0.9"},
                {"DISPLAYSOUNDLIST", "0"},
                {"EDGEFRICTION", "2"},
                {"EX_EXTRAPMAX", "1.2"},
                {"EX_INTERP", "0.1"},
                {"FAKELAG", "0.0"},
                {"FAKELOSS", "0.0"},
                {"FPS_OVERRIDE", "0"},
                {"FS_LAZY_PRECACHE", "0"},
                {"FS_PERF_WARNINGS", "0"},
                {"FS_PRECACHE_TIMINGS", "0"},
                {"FS_STARTUP_TIMINGS", "0"},
                {"GL_AFFINEMODELS", "0"},
                {"GL_ALPHAMIN", "0.25"},
                {"GL_CULL", "1"},
                {"GL_DITHER", "1"},
                {"GL_FLIPMATRIX", "0"},
                {"GL_FOG", "1"},
                {"GL_KEEPTJUNCTIONS", "1"},
                {"GL_LIGHTHOLES", "1"},
                {"GL_MONOLIGHTS", "0"},
                {"GL_NOBIND", "0"},
                {"GL_NOCOLORS", "0"},
                {"GL_PLAYERMIP", "0"},
                {"GL_REPORTTJUNCTIONS", "0"},
                {"GL_WIREFRAME", "0"},
                {"GL_ZTRICK", "0"},
                {"HOST_FRAMERATE", "0"},
                {"HOST_KILLTIME", "0"},
                {"HOST_LIMITLOCAL", "0"},
                {"HOST_PROFILE", "0"},
                {"HOST_SPEEDS", "0"},
                {"HOSTNAME", "HALF-LIFE"},
                {"HOSTPORT", "0"},
                {"IP", "LOCALHOST"},
                {"IP_CLIENTPORT", "0"},
                {"IP_HOSTPORT", "0"},
                {"IPX_CLIENTPORT", "0"},
                {"IPX_HOSTPORT", "0"},
                {"JOYADVANCED", "0"},
                {"JOYADVAXISR", "0"},
                {"JOYADVAXISU", "0"},
                {"JOYADVAXISV", "0"},
                {"JOYADVAXISX", "0"},
                {"JOYADVAXISY", "0"},
                {"JOYADVAXISZ", "0"},
                {"JOYFORWARDSENSITIVITY", "-1.0"},
                {"JOYFORWARDTHRESHOLD", "0.15"},
                {"JOYPITCHSENSITIVITY", "1.0"},
                {"JOYPITCHTHRESHOLD", "0.15"},
                {"JOYSIDESENSITIVITY", "-1.0"},
                {"JOYSIDETHRESHOLD", "0.15"},
                {"JOYWWHACK1", "0.0"},
                {"JOYWWHACK2", "0.0"},
                {"JOYYAWSENSITIVITY", "-1.0"},
                {"JOYYAWTHRESHOLD", "0.15"},
                {"LAMBERT", "1.5"},
                {"LOGSDIR", "LOGS"},
                {"LOOKSPRING", "0.000000"},
                {"LOOKSTRAFE", "0.000000"},
                {"MAPCYCLEFILE", "MAPCYCLE.TXT"},
                {"MAX_QUERIES_SEC", "3.0"},
                {"MAX_QUERIES_SEC_GLOBAL", "30"},
                {"MAX_QUERIES_WINDOW", "60"},
                {"MOTDFILE", "MOTD.TXT"},
                {"MP_ALLOWMONSTERS", "0"},
                {"MP_CHATTIME", "10"},
                {"MP_CONSISTENCY", "1"},
                {"MP_DEFAULTTEAM", "0"},
                {"MP_FALLDAMAGE", "0"},
                {"MP_FLASHLIGHT", "0"},
                {"MP_FOOTSTEPS", "1"},
                {"MP_FORCERESPAWN", "1"},
                {"MP_FRAGLIMIT", "0"},
                {"MP_FRAGSLEFT", "0"},
                {"MP_FRIENDLYFIRE", "0"},
                {"MP_LOGECHO", "1"},
                {"MP_LOGFILE", "1"},
                {"MP_TEAMOVERRIDE", "1"},
                {"MP_TEAMPLAY", "0"},
                {"MP_TIMELEFT", "0"},
                {"MP_TIMELIMIT", "0"},
                {"MP_WEAPONSTAY", "0"},
                {"MULTICASTPORT", "27025"},
                {"NET_CHOKELOOP", "0"},
                {"NET_DRAWSLIDER", "0"},
                {"NET_LOG", "0"},
                {"NET_SHOWDROP", "0"},
                {"NET_SHOWPACKETS", "0"},
                {"R_BMODELHIGHFRAC", "5.0"},
                {"R_DRAWENTITIES", "1"},
                {"R_FULLBRIGHT", "0"},
                {"R_SPEEDS", "0"},
                {"S_SHOW", "0"},
                {"S_SHOWTOSSED", "0"},
                {"SCR_CENTERTIME", "2"},
                {"SCR_CONNECTMSG", "0"},
                {"SCR_CONNECTMSG1", "0"},
                {"SCR_CONNECTMSG2", "0"},
                {"SCR_CONSPEED", "600"},
                {"SCR_OFSX", "0"},
                {"SCR_OFSY", "0"},
                {"SCR_OFSZ", "0"},
                {"SCR_PRINTSPEED", "8"},
                {"SKILL", "1"},
                {"SND_SHOW", "0"},
                {"SV_ACCELERATE", "10"},
                {"SV_AIRACCELERATE", "10"},
                {"SV_AIRMOVE", "1"},
                {"SV_AUTORECORD", "県"},
                {"SV_BOUNCE", "1"},
                {"SV_CHEATS", "0"},
                {"SV_CLIPMODE", "0"},
                {"SV_EXPLOSION_DISPLAY", "県"},
                {"SV_FRICTION", "4"},
                {"SV_GRAVITY", "800"},
                {"SV_LAN", "1"},
                {"SV_LOG_ONEFILE", "0"},
                {"SV_LOG_SINGLEPLAYER", "0"},
                {"SV_LOGBANS", "0"},
                {"SV_LOGBLOCKS", "0"},
                {"SV_LOGRELAY", "0"},
                {"SV_MAXSPEED", "320"},
                {"SV_MAXUNLAG", "0.5"},
                {"SV_MAXVELOCITY", "2000"},
                {"SV_NEWUNIT", "0"},
                {"SV_RCON_BANPENALTY", "0"},
                {"SV_RCON_MAXFAILURES", "10"},
                {"SV_RCON_MINFAILURES", "5"},
                {"SV_RCON_MINFAILURETIME", "30"},
                {"SV_REGION", "-1"},
                {"SV_SPECTATORMAXSPEED", "500"},
                {"SV_STATS", "1"},
                {"SV_STEPSIZE", "18"},
                {"SV_STOPSPEED", "100"},
                {"SV_UNLAGPUSH", "0.0"},
                {"SV_VOICECODEC", "VOICE_MILES"},
                {"SV_VOICEQUALITY", "3"},
                {"SV_WATERACCELERATE", "10"},
                {"SV_WATERAMP", "0"},
                {"SV_WATERFRICTION", "1"},
                {"SYS_TICRATE", "100.0"},
                {"VID_D3D", "0"}
            };

            // Add scriptless-specific cvars only in scriptless mode
            if (isScriptlessMode)
            {
                cvarRules.Add("BXT_AUTOPAUSE", "0");
                cvarRules.Add("BXT_AUTOJUMP", "0");
                cvarRules.Add("BXT_AUTOJUMP_PREDICTION", "0");
                cvarRules.Add("CL_PITCHDOWN", "89");
                cvarRules.Add("CL_PITCHUP", "89");
            }

            var skillCvarRules = new Dictionary<string, string>()
            {
                {"SK_12MM_BULLET1", "8"},
                {"SK_12MM_BULLET2", "10"},
                {"SK_12MM_BULLET3", "10"},
                {"SK_9MM_BULLET1", "5"},
                {"SK_9MM_BULLET2", "5"},
                {"SK_9MM_BULLET3", "8"},
                {"SK_9MMAR_BULLET1", "3"},
                {"SK_9MMAR_BULLET2", "4"},
                {"SK_9MMAR_BULLET3", "5"},
                {"SK_AGRUNT_DMG_PUNCH1", "10"},
                {"SK_AGRUNT_DMG_PUNCH2", "20"},
                {"SK_AGRUNT_DMG_PUNCH3", "20"},
                {"SK_AGRUNT_HEALTH1", "60"},
                {"SK_AGRUNT_HEALTH2", "90"},
                {"SK_AGRUNT_HEALTH3", "120"},
                {"SK_APACHE_HEALTH1", "150"},
                {"SK_APACHE_HEALTH2", "250"},
                {"SK_APACHE_HEALTH3", "400"},
                {"SK_BABYVOLTIGORE_DMG_PUNCH1", "10"},
                {"SK_BABYVOLTIGORE_DMG_PUNCH2", "15"},
                {"SK_BABYVOLTIGORE_DMG_PUNCH3", "20"},
                {"SK_BABYVOLTIGORE_HEALTH1", "60"},
                {"SK_BABYVOLTIGORE_HEALTH2", "60"},
                {"SK_BABYVOLTIGORE_HEALTH3", "120"},
                {"SK_BARNEY_HEALTH1", "35"},
                {"SK_BARNEY_HEALTH2", "35"},
                {"SK_BARNEY_HEALTH3", "35"},
                {"SK_BATTERY1", "15"},
                {"SK_BATTERY2", "15"},
                {"SK_BATTERY3", "10"},
                {"SK_BIGMOMMA_DMG_BLAST1", "100"},
                {"SK_BIGMOMMA_DMG_BLAST2", "120"},
                {"SK_BIGMOMMA_DMG_BLAST3", "160"},
                {"SK_BIGMOMMA_DMG_SLASH1", "50"},
                {"SK_BIGMOMMA_DMG_SLASH2", "60"},
                {"SK_BIGMOMMA_DMG_SLASH3", "70"},
                {"SK_BIGMOMMA_HEALTH_FACTOR1", "1.0"},
                {"SK_BIGMOMMA_HEALTH_FACTOR2", "1.5"},
                {"SK_BIGMOMMA_HEALTH_FACTOR3", "2"},
                {"SK_BIGMOMMA_RADIUS_BLAST1", "250"},
                {"SK_BIGMOMMA_RADIUS_BLAST2", "250"},
                {"SK_BIGMOMMA_RADIUS_BLAST3", "275"},
                {"SK_BULLSQUID_DMG_BITE1", "15"},
                {"SK_BULLSQUID_DMG_BITE2", "25"},
                {"SK_BULLSQUID_DMG_BITE3", "25"},
                {"SK_BULLSQUID_DMG_SPIT1", "10"},
                {"SK_BULLSQUID_DMG_SPIT2", "10"},
                {"SK_BULLSQUID_DMG_SPIT3", "15"},
                {"SK_BULLSQUID_DMG_WHIP1", "25"},
                {"SK_BULLSQUID_DMG_WHIP2", "35"},
                {"SK_BULLSQUID_DMG_WHIP3", "35"},
                {"SK_BULLSQUID_HEALTH1", "40"},
                {"SK_BULLSQUID_HEALTH2", "40"},
                {"SK_BULLSQUID_HEALTH3", "120"},
                {"SK_CLEANSUIT_SCIENTIST_HEAL1", "25"},
                {"SK_CLEANSUIT_SCIENTIST_HEAL2", "25"},
                {"SK_CLEANSUIT_SCIENTIST_HEAL3", "25"},
                {"SK_CLEANSUIT_SCIENTIST_HEALTH1", "20"},
                {"SK_CLEANSUIT_SCIENTIST_HEALTH2", "20"},
                {"SK_CLEANSUIT_SCIENTIST_HEALTH3", "20"},
                {"SK_CONTROLLER_DMGBALL1", "3"},
                {"SK_CONTROLLER_DMGBALL2", "4"},
                {"SK_CONTROLLER_DMGBALL3", "5"},
                {"SK_CONTROLLER_DMGZAP1", "15"},
                {"SK_CONTROLLER_DMGZAP2", "25"},
                {"SK_CONTROLLER_DMGZAP3", "35"},
                {"SK_CONTROLLER_HEALTH1", "60"},
                {"SK_CONTROLLER_HEALTH2", "60"},
                {"SK_CONTROLLER_HEALTH3", "100"},
                {"SK_CONTROLLER_SPEEDBALL1", "650"},
                {"SK_CONTROLLER_SPEEDBALL2", "800"},
                {"SK_CONTROLLER_SPEEDBALL3", "1000"},
                {"SK_GARGANTUA_DMG_FIRE1", "3"},
                {"SK_GARGANTUA_DMG_FIRE2", "5"},
                {"SK_GARGANTUA_DMG_FIRE3", "5"},
                {"SK_GARGANTUA_DMG_SLASH1", "10"},
                {"SK_GARGANTUA_DMG_SLASH2", "30"},
                {"SK_GARGANTUA_DMG_SLASH3", "30"},
                {"SK_GARGANTUA_DMG_STOMP1", "50"},
                {"SK_GARGANTUA_DMG_STOMP2", "100"},
                {"SK_GARGANTUA_DMG_STOMP3", "100"},
                {"SK_GARGANTUA_HEALTH1", "800"},
                {"SK_GARGANTUA_HEALTH2", "800"},
                {"SK_GARGANTUA_HEALTH3", "1000"},
                {"SK_GENEWORM_DMG_HIT1", "20"},
                {"SK_GENEWORM_DMG_HIT2", "30"},
                {"SK_GENEWORM_DMG_HIT3", "40"},
                {"SK_GENEWORM_DMG_SPIT1", "10"},
                {"SK_GENEWORM_DMG_SPIT2", "20"},
                {"SK_GENEWORM_DMG_SPIT3", "30"},
                {"SK_GENEWORM_HEALTH1", "70"},
                {"SK_GENEWORM_HEALTH2", "90"},
                {"SK_GENEWORM_HEALTH3", "100"},
                {"SK_GONOME_DMG_GUTS1", "10"},
                {"SK_GONOME_DMG_GUTS2", "10"},
                {"SK_GONOME_DMG_GUTS3", "15"},
                {"SK_GONOME_DMG_ONE_BITE1", "7"},
                {"SK_GONOME_DMG_ONE_BITE2", "14"},
                {"SK_GONOME_DMG_ONE_BITE3", "14"},
                {"SK_GONOME_DMG_ONE_SLASH1", "10"},
                {"SK_GONOME_DMG_ONE_SLASH2", "20"},
                {"SK_GONOME_DMG_ONE_SLASH3", "20"},
                {"SK_GONOME_HEALTH1", "85"},
                {"SK_GONOME_HEALTH2", "85"},
                {"SK_GONOME_HEALTH3", "160"},
                {"SK_HASSASSIN_HEALTH1", "30"},
                {"SK_HASSASSIN_HEALTH2", "50"},
                {"SK_HASSASSIN_HEALTH3", "50"},
                {"SK_HEADCRAB_DMG_BITE1", "5"},
                {"SK_HEADCRAB_DMG_BITE2", "10"},
                {"SK_HEADCRAB_DMG_BITE3", "10"},
                {"SK_HEADCRAB_HEALTH1", "10"},
                {"SK_HEADCRAB_HEALTH2", "10"},
                {"SK_HEADCRAB_HEALTH3", "20"},
                {"SK_HEALTHCHARGER1", "50"},
                {"SK_HEALTHCHARGER2", "40"},
                {"SK_HEALTHCHARGER3", "25"},
                {"SK_HEALTHKIT1", "15"},
                {"SK_HEALTHKIT2", "15"},
                {"SK_HEALTHKIT3", "10"},
                {"SK_HGRUNT_ALLY_GSPEED1", "600"},
                {"SK_HGRUNT_ALLY_GSPEED2", "600"},
                {"SK_HGRUNT_ALLY_GSPEED3", "600"},
                {"SK_HGRUNT_ALLY_HEALTH1", "50"},
                {"SK_HGRUNT_ALLY_HEALTH2", "50"},
                {"SK_HGRUNT_ALLY_HEALTH3", "50"},
                {"SK_HGRUNT_ALLY_KICK1", "10"},
                {"SK_HGRUNT_ALLY_KICK2", "10"},
                {"SK_HGRUNT_ALLY_KICK3", "5"},
                {"SK_HGRUNT_ALLY_PELLETS1", "5"},
                {"SK_HGRUNT_ALLY_PELLETS2", "5"},
                {"SK_HGRUNT_ALLY_PELLETS3", "3"},
                {"SK_HGRUNT_GSPEED1", "400"},
                {"SK_HGRUNT_GSPEED2", "600"},
                {"SK_HGRUNT_GSPEED3", "800"},
                {"SK_HGRUNT_HEALTH1", "50"},
                {"SK_HGRUNT_HEALTH2", "50"},
                {"SK_HGRUNT_HEALTH3", "80"},
                {"SK_HGRUNT_KICK1", "5"},
                {"SK_HGRUNT_KICK2", "10"},
                {"SK_HGRUNT_KICK3", "10"},
                {"SK_HGRUNT_PELLETS1", "3"},
                {"SK_HGRUNT_PELLETS2", "5"},
                {"SK_HGRUNT_PELLETS3", "6"},
                {"SK_HORNET_DMG1", "4"},
                {"SK_HORNET_DMG2", "5"},
                {"SK_HORNET_DMG3", "8"},
                {"SK_HOUNDEYE_DMG_BLAST1", "10"},
                {"SK_HOUNDEYE_DMG_BLAST2", "15"},
                {"SK_HOUNDEYE_DMG_BLAST3", "15"},
                {"SK_HOUNDEYE_HEALTH1", "20"},
                {"SK_HOUNDEYE_HEALTH2", "20"},
                {"SK_HOUNDEYE_HEALTH3", "30"},
                {"SK_ICHTHYOSAUR_HEALTH1", "200"},
                {"SK_ICHTHYOSAUR_HEALTH2", "200"},
                {"SK_ICHTHYOSAUR_HEALTH3", "400"},
                {"SK_ICHTHYOSAUR_SHAKE1", "20"},
                {"SK_ICHTHYOSAUR_SHAKE2", "35"},
                {"SK_ICHTHYOSAUR_SHAKE3", "50"},
                {"SK_ISLAVE_DMG_CLAW1", "8"},
                {"SK_ISLAVE_DMG_CLAW2", "10"},
                {"SK_ISLAVE_DMG_CLAW3", "10"},
                {"SK_ISLAVE_DMG_CLAWRAKE1", "25"},
                {"SK_ISLAVE_DMG_CLAWRAKE2", "25"},
                {"SK_ISLAVE_DMG_CLAWRAKE3", "25"},
                {"SK_ISLAVE_DMG_ZAP1", "10"},
                {"SK_ISLAVE_DMG_ZAP2", "10"},
                {"SK_ISLAVE_DMG_ZAP3", "15"},
                {"SK_ISLAVE_HEALTH1", "30"},
                {"SK_ISLAVE_HEALTH2", "30"},
                {"SK_ISLAVE_HEALTH3", "60"},
                {"SK_LEECH_DMG_BITE1", "2"},
                {"SK_LEECH_DMG_BITE2", "2"},
                {"SK_LEECH_DMG_BITE3", "2"},
                {"SK_LEECH_HEALTH1", "2"},
                {"SK_LEECH_HEALTH2", "2"},
                {"SK_LEECH_HEALTH3", "2"},
                {"SK_MASSASSIN_GSPEED1", "400"},
                {"SK_MASSASSIN_GSPEED2", "600"},
                {"SK_MASSASSIN_GSPEED3", "800"},
                {"SK_MASSASSIN_HEALTH1", "50"},
                {"SK_MASSASSIN_HEALTH2", "50"},
                {"SK_MASSASSIN_HEALTH3", "80"},
                {"SK_MASSASSIN_KICK1", "15"},
                {"SK_MASSASSIN_KICK2", "25"},
                {"SK_MASSASSIN_KICK3", "25"},
                {"SK_MEDIC_ALLY_GSPEED1", "600"},
                {"SK_MEDIC_ALLY_GSPEED2", "600"},
                {"SK_MEDIC_ALLY_GSPEED3", "600"},
                {"SK_MEDIC_ALLY_HEAL1", "200"},
                {"SK_MEDIC_ALLY_HEAL2", "120"},
                {"SK_MEDIC_ALLY_HEAL3", "75"},
                {"SK_MEDIC_ALLY_HEALTH1", "50"},
                {"SK_MEDIC_ALLY_HEALTH2", "50"},
                {"SK_MEDIC_ALLY_HEALTH3", "50"},
                {"SK_MEDIC_ALLY_KICK1", "10"},
                {"SK_MEDIC_ALLY_KICK2", "10"},
                {"SK_MEDIC_ALLY_KICK3", "5"},
                {"SK_MINITURRET_HEALTH1", "40"},
                {"SK_MINITURRET_HEALTH2", "40"},
                {"SK_MINITURRET_HEALTH3", "50"},
                {"SK_MONSTER_ARM1", "1"},
                {"SK_MONSTER_ARM2", "1"},
                {"SK_MONSTER_ARM3", "1"},
                {"SK_MONSTER_CHEST1", "1"},
                {"SK_MONSTER_CHEST2", "1"},
                {"SK_MONSTER_CHEST3", "1"},
                {"SK_MONSTER_HEAD1", "3"},
                {"SK_MONSTER_HEAD2", "3"},
                {"SK_MONSTER_HEAD3", "3"},
                {"SK_MONSTER_LEG1", "1"},
                {"SK_MONSTER_LEG2", "1"},
                {"SK_MONSTER_LEG3", "1"},
                {"SK_MONSTER_STOMACH1", "1"},
                {"SK_MONSTER_STOMACH2", "1"},
                {"SK_MONSTER_STOMACH3", "1"},
                {"SK_NIHILANTH_HEALTH1", "800"},
                {"SK_NIHILANTH_HEALTH2", "800"},
                {"SK_NIHILANTH_HEALTH3", "1000"},
                {"SK_NIHILANTH_ZAP1", "30"},
                {"SK_NIHILANTH_ZAP2", "30"},
                {"SK_NIHILANTH_ZAP3", "50"},
                {"SK_OTIS_HEALTH1", "35"},
                {"SK_OTIS_HEALTH2", "35"},
                {"SK_OTIS_HEALTH3", "35"},
                {"SK_PITDRONE_DMG_BITE1", "15"},
                {"SK_PITDRONE_DMG_BITE2", "25"},
                {"SK_PITDRONE_DMG_BITE3", "25"},
                {"SK_PITDRONE_DMG_SPIT1", "10"},
                {"SK_PITDRONE_DMG_SPIT2", "10"},
                {"SK_PITDRONE_DMG_SPIT3", "15"},
                {"SK_PITDRONE_DMG_WHIP1", "25"},
                {"SK_PITDRONE_DMG_WHIP2", "35"},
                {"SK_PITDRONE_DMG_WHIP3", "35"},
                {"SK_PITDRONE_HEALTH1", "40"},
                {"SK_PITDRONE_HEALTH2", "40"},
                {"SK_PITDRONE_HEALTH3", "110"},
                {"SK_PITWORM_DMG_BEAM1", "10"},
                {"SK_PITWORM_DMG_BEAM2", "15"},
                {"SK_PITWORM_DMG_BEAM3", "20"},
                {"SK_PITWORM_DMG_SWIPE1", "30"},
                {"SK_PITWORM_DMG_SWIPE2", "40"},
                {"SK_PITWORM_DMG_SWIPE3", "50"},
                {"SK_PITWORM_HEALTH1", "70"},
                {"SK_PITWORM_HEALTH2", "90"},
                {"SK_PITWORM_HEALTH3", "100"},
                {"SK_PLAYER_ARM1", "1"},
                {"SK_PLAYER_ARM2", "1"},
                {"SK_PLAYER_ARM3", "1"},
                {"SK_PLAYER_CHEST1", "1"},
                {"SK_PLAYER_CHEST2", "1"},
                {"SK_PLAYER_CHEST3", "1"},
                {"SK_PLAYER_HEAD1", "3"},
                {"SK_PLAYER_HEAD2", "3"},
                {"SK_PLAYER_HEAD3", "3"},
                {"SK_PLAYER_LEG1", "1"},
                {"SK_PLAYER_LEG2", "1"},
                {"SK_PLAYER_LEG3", "1"},
                {"SK_PLAYER_STOMACH1", "1"},
                {"SK_PLAYER_STOMACH2", "1"},
                {"SK_PLAYER_STOMACH3", "1"},
                {"SK_PLR_357_BULLET1", "40"},
                {"SK_PLR_357_BULLET2", "40"},
                {"SK_PLR_357_BULLET3", "40"},
                {"SK_PLR_556_BULLET1", "15"},
                {"SK_PLR_556_BULLET2", "15"},
                {"SK_PLR_556_BULLET3", "15"},
                {"SK_PLR_762_BULLET1", "100"},
                {"SK_PLR_762_BULLET2", "100"},
                {"SK_PLR_762_BULLET3", "100"},
                {"SK_PLR_9MM_BULLET1", "8"},
                {"SK_PLR_9MM_BULLET2", "8"},
                {"SK_PLR_9MM_BULLET3", "8"},
                {"SK_PLR_9MMAR_BULLET1", "5"},
                {"SK_PLR_9MMAR_BULLET2", "5"},
                {"SK_PLR_9MMAR_BULLET3", "5"},
                {"SK_PLR_9MMAR_GRENADE1", "100"},
                {"SK_PLR_9MMAR_GRENADE2", "100"},
                {"SK_PLR_9MMAR_GRENADE3", "100"},
                {"SK_PLR_BUCKSHOT1", "5"},
                {"SK_PLR_BUCKSHOT2", "5"},
                {"SK_PLR_BUCKSHOT3", "5"},
                {"SK_PLR_CROWBAR1", "10"},
                {"SK_PLR_CROWBAR2", "10"},
                {"SK_PLR_CROWBAR3", "10"},
                {"SK_PLR_DISPLACER_OTHER1", "250"},
                {"SK_PLR_DISPLACER_OTHER2", "250"},
                {"SK_PLR_DISPLACER_OTHER3", "250"},
                {"SK_PLR_DISPLACER_RADIUS1", "300"},
                {"SK_PLR_DISPLACER_RADIUS2", "300"},
                {"SK_PLR_DISPLACER_RADIUS3", "300"},
                {"SK_PLR_DISPLACER_SELF1", "5"},
                {"SK_PLR_DISPLACER_SELF2", "5"},
                {"SK_PLR_DISPLACER_SELF3", "5"},
                {"SK_PLR_EAGLE1", "34"},
                {"SK_PLR_EAGLE2", "34"},
                {"SK_PLR_EAGLE3", "34"},
                {"SK_PLR_EGON_NARROW1", "6"},
                {"SK_PLR_EGON_NARROW2", "6"},
                {"SK_PLR_EGON_NARROW3", "6"},
                {"SK_PLR_EGON_WIDE1", "14"},
                {"SK_PLR_EGON_WIDE2", "14"},
                {"SK_PLR_EGON_WIDE3", "14"},
                {"SK_PLR_GAUSS1", "20"},
                {"SK_PLR_GAUSS2", "20"},
                {"SK_PLR_GAUSS3", "20"},
                {"SK_PLR_GRAPPLE1", "25"},
                {"SK_PLR_GRAPPLE2", "25"},
                {"SK_PLR_GRAPPLE3", "25"},
                {"SK_PLR_HAND_GRENADE1", "100"},
                {"SK_PLR_HAND_GRENADE2", "100"},
                {"SK_PLR_HAND_GRENADE3", "100"},
                {"SK_PLR_KNIFE1", "10"},
                {"SK_PLR_KNIFE2", "10"},
                {"SK_PLR_KNIFE3", "10"},
                {"SK_PLR_PIPEWRENCH1", "20"},
                {"SK_PLR_PIPEWRENCH2", "20"},
                {"SK_PLR_PIPEWRENCH3", "20"},
                {"SK_PLR_RPG1", "100"},
                {"SK_PLR_RPG2", "100"},
                {"SK_PLR_RPG3", "100"},
                {"SK_PLR_SATCHEL1", "150"},
                {"SK_PLR_SATCHEL2", "150"},
                {"SK_PLR_SATCHEL3", "150"},
                {"SK_PLR_SHOCKROACHM1", "15"},
                {"SK_PLR_SHOCKROACHM2", "15"},
                {"SK_PLR_SHOCKROACHM3", "15"},
                {"SK_PLR_SHOCKROACHS1", "10"},
                {"SK_PLR_SHOCKROACHS2", "10"},
                {"SK_PLR_SHOCKROACHS3", "10"},
                {"SK_PLR_SPORE1", "50"},
                {"SK_PLR_SPORE2", "50"},
                {"SK_PLR_SPORE3", "50"},
                {"SK_PLR_TRIPMINE1", "150"},
                {"SK_PLR_TRIPMINE2", "150"},
                {"SK_PLR_TRIPMINE3", "150"},
                {"SK_PLR_XBOW_BOLT_CLIENT1", "10"},
                {"SK_PLR_XBOW_BOLT_CLIENT2", "10"},
                {"SK_PLR_XBOW_BOLT_CLIENT3", "10"},
                {"SK_PLR_XBOW_BOLT_MONSTER1", "50"},
                {"SK_PLR_XBOW_BOLT_MONSTER2", "50"},
                {"SK_PLR_XBOW_BOLT_MONSTER3", "50"},
                {"SK_SCIENTIST_HEAL1", "25"},
                {"SK_SCIENTIST_HEAL2", "25"},
                {"SK_SCIENTIST_HEAL3", "25"},
                {"SK_SCIENTIST_HEALTH1", "20"},
                {"SK_SCIENTIST_HEALTH2", "20"},
                {"SK_SCIENTIST_HEALTH3", "20"},
                {"SK_SENTRY_HEALTH1", "40"},
                {"SK_SENTRY_HEALTH2", "40"},
                {"SK_SENTRY_HEALTH3", "50"},
                {"SK_SHOCKROACH_DMG_BITE1", "5"},
                {"SK_SHOCKROACH_DMG_BITE2", "10"},
                {"SK_SHOCKROACH_DMG_BITE3", "10"},
                {"SK_SHOCKROACH_HEALTH1", "10"},
                {"SK_SHOCKROACH_HEALTH2", "10"},
                {"SK_SHOCKROACH_HEALTH3", "20"},
                {"SK_SHOCKROACH_LIFESPAN1", "10"},
                {"SK_SHOCKROACH_LIFESPAN2", "10"},
                {"SK_SHOCKROACH_LIFESPAN3", "10"},
                {"SK_SHOCKTROOPER_GSPEED1", "400"},
                {"SK_SHOCKTROOPER_GSPEED2", "600"},
                {"SK_SHOCKTROOPER_GSPEED3", "800"},
                {"SK_SHOCKTROOPER_HEALTH1", "50"},
                {"SK_SHOCKTROOPER_HEALTH2", "50"},
                {"SK_SHOCKTROOPER_HEALTH3", "80"},
                {"SK_SHOCKTROOPER_KICK1", "5"},
                {"SK_SHOCKTROOPER_KICK2", "10"},
                {"SK_SHOCKTROOPER_KICK3", "10"},
                {"SK_SHOCKTROOPER_MAXCHARGE1", "8"},
                {"SK_SHOCKTROOPER_MAXCHARGE2", "8"},
                {"SK_SHOCKTROOPER_MAXCHARGE3", "8"},
                {"SK_SHOCKTROOPER_RCHGSPEED1", "1"},
                {"SK_SHOCKTROOPER_RCHGSPEED2", "1"},
                {"SK_SHOCKTROOPER_RCHGSPEED3", "1"},
                {"SK_SNARK_DMG_BITE1", "10"},
                {"SK_SNARK_DMG_BITE2", "10"},
                {"SK_SNARK_DMG_BITE3", "10"},
                {"SK_SNARK_DMG_POP1", "5"},
                {"SK_SNARK_DMG_POP2", "5"},
                {"SK_SNARK_DMG_POP3", "5"},
                {"SK_SNARK_HEALTH1", "2"},
                {"SK_SNARK_HEALTH2", "2"},
                {"SK_SNARK_HEALTH3", "2"},
                {"SK_SUITCHARGER1", "75"},
                {"SK_SUITCHARGER2", "50"},
                {"SK_SUITCHARGER3", "35"},
                {"SK_TORCH_ALLY_GSPEED1", "600"},
                {"SK_TORCH_ALLY_GSPEED2", "600"},
                {"SK_TORCH_ALLY_GSPEED3", "600"},
                {"SK_TORCH_ALLY_HEALTH1", "50"},
                {"SK_TORCH_ALLY_HEALTH2", "50"},
                {"SK_TORCH_ALLY_HEALTH3", "50"},
                {"SK_TORCH_ALLY_KICK1", "10"},
                {"SK_TORCH_ALLY_KICK2", "10"},
                {"SK_TORCH_ALLY_KICK3", "5"},
                {"SK_TURRET_HEALTH1", "50"},
                {"SK_TURRET_HEALTH2", "50"},
                {"SK_TURRET_HEALTH3", "60"},
                {"SK_VOLTIGORE_DMG_BEAM1", "40"},
                {"SK_VOLTIGORE_DMG_BEAM2", "50"},
                {"SK_VOLTIGORE_DMG_BEAM3", "60"},
                {"SK_VOLTIGORE_DMG_PUNCH1", "30"},
                {"SK_VOLTIGORE_DMG_PUNCH2", "40"},
                {"SK_VOLTIGORE_DMG_PUNCH3", "50"},
                {"SK_VOLTIGORE_HEALTH1", "320"},
                {"SK_VOLTIGORE_HEALTH2", "320"},
                {"SK_VOLTIGORE_HEALTH3", "450"},
                {"SK_ZOMBIE_BARNEY_DMG_BOTH_SLASH1", "25"},
                {"SK_ZOMBIE_BARNEY_DMG_BOTH_SLASH2", "40"},
                {"SK_ZOMBIE_BARNEY_DMG_BOTH_SLASH3", "40"},
                {"SK_ZOMBIE_BARNEY_DMG_ONE_SLASH1", "10"},
                {"SK_ZOMBIE_BARNEY_DMG_ONE_SLASH2", "20"},
                {"SK_ZOMBIE_BARNEY_DMG_ONE_SLASH3", "20"},
                {"SK_ZOMBIE_BARNEY_HEALTH1", "50"},
                {"SK_ZOMBIE_BARNEY_HEALTH2", "50"},
                {"SK_ZOMBIE_BARNEY_HEALTH3", "100"},
                {"SK_ZOMBIE_DMG_BOTH_SLASH1", "25"},
                {"SK_ZOMBIE_DMG_BOTH_SLASH2", "40"},
                {"SK_ZOMBIE_DMG_BOTH_SLASH3", "40"},
                {"SK_ZOMBIE_DMG_ONE_SLASH1", "10"},
                {"SK_ZOMBIE_DMG_ONE_SLASH2", "20"},
                {"SK_ZOMBIE_DMG_ONE_SLASH3", "20"},
                {"SK_ZOMBIE_HEALTH1", "50"},
                {"SK_ZOMBIE_HEALTH2", "50"},
                {"SK_ZOMBIE_HEALTH3", "100"},
                {"SK_ZOMBIE_SOLDIER_DMG_BOTH_SLASH1", "25"},
                {"SK_ZOMBIE_SOLDIER_DMG_BOTH_SLASH2", "40"},
                {"SK_ZOMBIE_SOLDIER_DMG_BOTH_SLASH3", "40"},
                {"SK_ZOMBIE_SOLDIER_DMG_ONE_SLASH1", "10"},
                {"SK_ZOMBIE_SOLDIER_DMG_ONE_SLASH2", "20"},
                {"SK_ZOMBIE_SOLDIER_DMG_ONE_SLASH3", "20"},
                {"SK_ZOMBIE_SOLDIER_HEALTH1", "60"},
                {"SK_ZOMBIE_SOLDIER_HEALTH2", "60"},
                {"SK_ZOMBIE_SOLDIER_HEALTH3", "120"},
            };
            if(info.Value.GsDemoInfo.Header.MapName.StartsWith("ba_"))  //blue shift map
            {
                skillCvarRules["SK_BATTERY1"] = "20";
                skillCvarRules["SK_BATTERY2"] = "20";
                skillCvarRules["SK_BATTERY3"] = "20";
            }

            string gamedir = info.Value.GsDemoInfo.Header.GameDir;
            bool gameLooksLikeTrilogy = gamedir.StartsWith("valve") || gamedir.StartsWith("gearbox") || gamedir.StartsWith("bshift") || gamedir.StartsWith("killcount");

            if (gameLooksLikeTrilogy)
            {
                skillCvarRules.ToList().ForEach(x => cvarRules.Add(x.Key, x.Value));
            }

            var demonode = new TreeNode(Path.GetFileName(info.Key)) { ForeColor = Color.LightCoral };
            bool gameEndReported = false;
            
            for (int i = 0; i < info.Value.GsDemoInfo.IncludedBXtData.Count; i++)
            {
                int jp = 0, jm = 0, dp = 0, dm = 0;
                var datanode = new TreeNode("\nBXT Data Frame [" + i + "]") { ForeColor = Color.LightPink };
                for (int index = 0; index < info.Value.GsDemoInfo.IncludedBXtData[i].Objects.Count; index++)
                {
                    KeyValuePair<Bxt.RuntimeDataType, Bxt.BXTData> t = info.Value.GsDemoInfo.IncludedBXtData[i].Objects[index];
                    switch (t.Key)
                    {
                        case Bxt.RuntimeDataType.VERSION_INFO:
                            {
                                textBuffer.Append("\t" + "BXT Version: " + ((((Bxt.VersionInfo)t.Value).bxt_version == bxtVersion) ? "Latest (November 11th 2024)" : ("INVALID=" + ((Bxt.VersionInfo)t.Value).bxt_version)) + "\n");
                                textBuffer.Append("\t" + "Game Version: " + ((Bxt.VersionInfo)t.Value).build_number + ", Game Directory: " + gamedir + "\n");
                                datanode.Nodes.Add(new TreeNode("Version info")
                                {
                                    ForeColor = Color.PaleVioletRed,
                                    Nodes =
                                    {
                                        new TreeNode("Game version: " + ((Bxt.VersionInfo) t.Value).build_number) { ForeColor = Color.PaleVioletRed },
                                        new TreeNode("BXT Version: " + ((Bxt.VersionInfo) t.Value).bxt_version) { ForeColor = Color.PaleVioletRed }
                                    },
                                });
                                break;
                            }
                        case Bxt.RuntimeDataType.CVAR_VALUES:
                            {
                                var cvars = ((Bxt.CVarValues)info.Value.GsDemoInfo.IncludedBXtData[i].Objects[index].Value).CVars;
                                if (cvars.Any(item  => item .Key == "bxt_bhopcap_prediction")) // only registered for steam
                                {
                                    cvarRules["BXT_BHOPCAP"] = "1";
                                    cvarRules.Remove("FPS_OVERRIDE");
                                }
                                foreach (var cvar in ((Bxt.CVarValues)t.Value).CVars.Where(cvar => cvarRules.ContainsKey(cvar.Key.ToUpper())).Where(cvar => cvarRules[cvar.Key.ToUpper()] != cvar.Value.ToUpper()))
                                {
                                    textBuffer.Append("\t" + "Illegal Cvar: " + cvar.Key + " " + cvar.Value + "\n", IllegalColor);
                                }
                                var cvarnode = new TreeNode("Cvars [" + ((Bxt.CVarValues)t.Value).CVars.Count + "]")
                                {
                                    ForeColor = Color.LightBlue
                                };
                                cvarnode.Nodes.AddRange(
                                    ((Bxt.CVarValues)t.Value).CVars.OrderBy(x => x.Key).Select(
                                        x => new TreeNode(x.Key + " " + x.Value) { ForeColor = Color.LightBlue }).ToArray());
                                datanode.Nodes.Add(cvarnode);
                                break;
                            }
                        case Bxt.RuntimeDataType.TIME:
                            {
                                if (i+1 == info.Value.GsDemoInfo.IncludedBXtData.Count)
                                {
                                    textBuffer.Append("\t" + "Demo bxt time: " + ((Bxt.Time)t.Value).ToString() + " — Frame: " + i + "\n");
                                }
                                datanode.Nodes.Add(new TreeNode("Time: " + ((Bxt.Time)t.Value).ToString())
                                {
                                    ForeColor = Color.Yellow
                                });
                                break;
                            }
                        case Bxt.RuntimeDataType.BOUND_COMMAND:
                            {
                                string command = ((Bxt.BoundCommand)t.Value).command.Trim();
                                if (isScriptlessMode)
                                {
                                    if (command.ToUpper().Contains("+JUMP"))
                                        jp++;
                                    if (command.ToUpper().Contains("-JUMP"))
                                        jm++;
                                    if (command.ToUpper().Contains("+DUCK"))
                                        dp++;
                                    if (command.ToUpper().Contains("-DUCK"))
                                        dm++;
                                    if (command.ToUpper().Contains(";"))
                                    {
                                        textBuffer.Append("\t" + "Possible script: " + command + " — Frame: " + i + "\n");
                                    }
                                    if (command.ToUpper().Contains("REPORT_TO_DEMO"))
                                    {
                                        textBuffer.Append("HL100: Illegal bound report_to_demo command!\n", IllegalColor);
                                        info.Value.GsDemoInfo.Cheats.Add(command);

                                    }
                                    datanode.Nodes.Add(new TreeNode("Bound command: " + command)
                                    {
                                        ForeColor = Color.LightSalmon
                                    });
                                }
                                break;
                            }
                        case Bxt.RuntimeDataType.ALIAS_EXPANSION:
                            {
                                string aliasCommand = ((Bxt.AliasExpansion)t.Value).command.Trim();     
                                if (isScriptlessMode)
                                {
                                    if (aliasCommand.ToUpper().Contains(";"))
                                    {
                                        textBuffer.Append("\t" + "Alias [" + ((Bxt.AliasExpansion)t.Value).name + "]: " + aliasCommand + " — Frame: " + i + "\n");
                                    }
                                }
                                else
                                {
                                    // Scripted: check for movement commands in aliases
                                    var moveCmds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) // case insensitive movement commands
                                    {
                                        "+left", "+right", "+forward", "+back", "+moveright", "+moveleft"
                                    };
                                    
                                    if (moveCmds.Any(cmd => aliasCommand.IndexOf(cmd, StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        string[] commands = aliasCommand.Split(';');
                                        textBuffer.Append("\t" + "Movement command in alias [" + ((Bxt.AliasExpansion)t.Value).name + "]: ");

                                        foreach (var cmd in commands)
                                        {
                                            string trimmedCmd = cmd.Trim();
                                            bool containsMove = moveCmds.Any(mc =>
                                                trimmedCmd.IndexOf(mc, StringComparison.OrdinalIgnoreCase) >= 0);

                                            textBuffer.Append(trimmedCmd, containsMove ? WarningColor : mrtb.ForeColor);
                                            textBuffer.Append("; ", mrtb.ForeColor);
                                        }

                                        textBuffer.Append("— Frame: " + i + "\n");
                                    }
                                }
                                
                                datanode.Nodes.Add(new TreeNode("Alias [" + ((Bxt.AliasExpansion)t.Value).name + "]: " + aliasCommand) { ForeColor = Color.LightCyan });
                                break;
                            }
                        case Bxt.RuntimeDataType.SCRIPT_EXECUTION:
                            {
                                // NOTE: When there is already enough data in a console command buffer and a config gets executed
                                // (for example, a config inside a config), if the combined size becomes more than 16 kb of data (cmd_text.maxsize),
                                // that 2nd config will get skipped and stuffed to the end, thus breaking the order of execution.
                                // case in point -- https://www.speedrun.com/hl1/runs/zqd4kv5m

                                textBuffer.Append("\t" + "Config execution: " + ((Bxt.ScriptExecution)t.Value).filename + " — Frame: " + i + "\n");

                                // TODO: If someone has their scripts in a sub directory and half-life's cmd_exec_f() gets called
                                // with e.g. "exec scripts/gauss.cfg", File.WriteAllText() will throw an IO exception.


                                //Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\verification cfgs\\");
                                //if (((Bxt.ScriptExecution)t.Value).filename == "")
                                //{
                                //    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\verification cfgs\\" + "broken_settings.cfg", ((Bxt.ScriptExecution)t.Value).contents);
                                //}
                                //else
                                //{
                                //    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\verification cfgs\\" + ((Bxt.ScriptExecution)t.Value).filename, ((Bxt.ScriptExecution)t.Value).contents);
                                //}
                                datanode.Nodes.Add(new TreeNode("Script: " + ((Bxt.ScriptExecution)t.Value).filename)
                                {
                                    ForeColor = Color.LightSteelBlue,
                                    Nodes =
                                    {
                                        new TreeNode(((Bxt.ScriptExecution) t.Value).contents) {ForeColor = Color.LightSteelBlue}
                                    }
                                });
                                break;
                            }
                        case Bxt.RuntimeDataType.COMMAND_EXECUTION:
                        {
                            string command = ((Bxt.CommandExecution)t.Value).command.Trim();
                            if (command.ToUpper().StartsWith("BIND") || command.ToUpper().StartsWith("ALIAS"))
                            {
                                break;  // Creating any binds/aliases are legal. Check legality during expansion/execution
                            }
                            //autojump detection scriptless
                            if (isScriptlessMode)
                            {
                                if (command.ToUpper().Contains("+JUMP"))
                                {
                                    if (jp == 0)
                                        textBuffer.Append("\t" + "Possible autojump: " + command + " — Frame: " + i + "\n");
                                    else
                                        jp--;
                                }
                                if (command.ToUpper().Contains("-JUMP"))
                                {
                                    if (jm == 0)
                                        textBuffer.Append("\t" + "Possible autojump: " + command + " — Frame: " + i + "\n");
                                    else
                                        jm--;
                                }
                                if (command.ToUpper().Contains("+DUCK"))
                                {
                                    if (dp == 0)
                                        textBuffer.Append("\t" + "Possible ducktap: " + command + " — Frame: " + i + "\n");
                                    else
                                        dp--;
                                }
                                if (command.ToUpper().Contains("-DUCK"))
                                {
                                    if (dm == 0)
                                        textBuffer.Append("\t" + "Possible ducktap: " + command + " — Frame: " + i + "\n");
                                    else
                                        dm--;
                                }
                            }

                            datanode.Nodes.Add(new TreeNode("Command: " + command)
                            {
                                ForeColor = Color.LightGreen
                            });

                            if (command.ToUpper().Contains("BXT"))
                            {
                                var allowedBxtCommands = new HashSet<string>
                                {
                                    "_HUD_TIMER",
                                    "_HUD_COLOR",
                                    "_HUD_JUMPSPEED",
                                    "_HUD_SPEEDOMETER",
                                    "_HUD_VIEWANGLES",
                                    "_HUD_INCORRECT_FPS",
                                    "_HUD_GAME",
                                    "_DISABLE_NIGHTVISION_SPRITE",
                                    "_DISABLE_AUTOSAVE",
                                    "_CROSS",
                                    "_VIEWMODEL",
                                    "_FIX_WIDESCREEN_FOV"
                                };

                                if (!isScriptlessMode)
                                {
                                    allowedBxtCommands.Add("_JUMPBUG");
                                    allowedBxtCommands.Add("_AUTOJUMP");
                                    allowedBxtCommands.Add("_DUCKTAP");
                                    allowedBxtCommands.Add("_APPEND");
                                }

                                bool isAllowed = allowedBxtCommands.Any(allowed => command.ToUpper().Contains(allowed));

                                if (!isAllowed)
                                {
                                    textBuffer.Append("\t" + "Disallowed BXT command: " + command + " — Frame: " + i + "\n", IllegalColor);
                                }
                            }
                            if (command.ToUpper().StartsWith("LOAD"))
                            {
                                string loadName = command.Substring(4).ToUpper().Trim();
                                    if(loadName == "QUICK" || loadName == "HARD" || loadName == "AUTOSAVE")
                                {
                                    textBuffer.Append("\t" + command + "\n");
                                }
                                else
                                {
                                    textBuffer.Append("\t" + command + "\n", WarningColor);
                                }
                            }
                            if (command.ToUpper().Contains("CUST_"))
                            {
                                //TODO: Verify based on currently equipped weapon
                                var allowedCust = new HashSet<string>
                                {
                                    "CUST_11", "CUST_12", "CUST_13", "CUST_14", "CUST_15",
                                    "CUST_21", "CUST_22", "CUST_23", "CUST_24", "CUST_25",
                                    "CUST_31", "CUST_32", "CUST_33", "CUST_34", "CUST_35",
                                    "CUST_41", "CUST_42", "CUST_43", "CUST_44", "CUST_45"
                                };

                                if (!allowedCust.Any(c => command.ToUpper().Contains(c)))
                                {
                                    textBuffer.Append("\t" + "cust value out of range: " + command + " — Frame: " + i + "\n", IllegalColor);
                                }
                            }
                            if (!command.ToUpper().StartsWith("REPORT_TO_DEMO") &&
                                   ( command.ToUpper().Contains("HOST_")
                                    || command.ToUpper().Contains("SK_")
                                    || command.ToUpper().Contains("CHASE")
                                    || command.ToUpper().Contains("SKILL")
                                    || (command.ToUpper().Contains("WAIT") && isScriptlessMode)
                                    || command.ToUpper().Contains("CONNECT")
                                    || command.ToUpper().Contains("DELTA")
                                    || command.ToUpper().Contains("EDGEFRICTION")
                                    || command.ToUpper().Contains("FS_")
                                    || command.ToUpper().Contains("MAPCHANGECFGFILE")
                                    || command.ToUpper().Contains("NOTARGET")
                                    || command.ToUpper().Contains("PLAYDEMO")
                                    || command.ToUpper().Contains("S_SHOW")
                                    || command.ToUpper().Contains("SPEC_POS")
                                    || command.ToUpper().Contains("THIRDPERSON")
                                    || command.ToUpper().Contains("SCR_")
                                    || command.ToUpper().StartsWith("C_")
                                    || command.ToUpper().Contains("CAM")
                                  || command.ToUpper().Contains("JOY")))
                            {
                                textBuffer.Append("\t" + "Disallowed: " + command + " — Frame: " + i + "\n", IllegalColor);
                            }
                                if ((command.ToUpper().Contains("SV_")
                                  && !command.ToUpper().Contains("AIM"))

                                  || (command.ToUpper().Contains("CL_")
                                && !(
                                        command.ToUpper().Contains("BOB")
                                    || command.ToUpper().Contains("SHOWFPS")
                                    || command.ToUpper().Contains("RIGHTHAND")
                                    || (!isScriptlessMode &&
                                        (command.ToUpper().Contains("PITCHDOWN")
                                        || command.ToUpper().Contains("PITCHUP")))
                                    ))
                                || command.ToUpper().StartsWith("MP_")
                                || command.ToUpper().StartsWith("R_")

                                  || (command.ToUpper().Contains("GL_")
                                  && !command.ToUpper().Contains("TEXTUREMODE"))

                                  || command.ToUpper().StartsWith("STAT"))
                                {
                                    textBuffer.Append("\t" + "Probably disallowed ¯\\_(ツ)_/¯: " + command + " — Frame: " + i + "\n");
                                }
                                if (command.ToUpper().Contains("REPORT_TO_DEMO"))
                                {
                                    try
                                    {
                                        // Special HL100 command to report kills to demo
                                        string[] tokens = command.Split();
                                        string monsterType = tokens[1];
                                        string monsterName = tokens[2].Substring(1, tokens[2].Length - 2); // may be empty
                                        string monsterKilledOnMap = tokens[4];
                                        int monsterKillNumber = int.Parse(tokens[6]);

                                        // Check if this kill was already gotten (implies save-reload)
                                        if (MonsterTypeKillByNumber.ContainsKey(monsterKillNumber))
                                        {
                                            // Get the UUID of the old kill that was invalidated by save-reload
                                            string oldUUID = MonsterTypeKillByNumber[monsterKillNumber].Item1;

                                            // Remove from dicts
                                            MonsterTypeKillByNumber.Remove(monsterKillNumber);
                                            
                                            foreach (var map in MonsterTypeKillByMap)
                                            {
                                                for (int j = 0; j < MonsterTypeKillByMap[map.Key].Count(); j++)
                                                {
                                                    if (MonsterTypeKillByMap[map.Key][j].Item1 == oldUUID)
                                                    {
                                                        MonsterTypeKillByMap[map.Key].RemoveAt(j);
                                                        j--;
                                                    }
                                                }
                                            }
                                        }

                                        string killUUID = Guid.NewGuid().ToString();
                                        MonsterTypeKillByNumber[monsterKillNumber] = (killUUID, monsterType, monsterName, monsterKilledOnMap);
                                        if (!MonsterTypeKillByMap.ContainsKey(monsterKilledOnMap))
                                        {
                                            MonsterTypeKillByMap.Add(monsterKilledOnMap, new List<(string, string)>());
                                        }
                                        MonsterTypeKillByMap[monsterKilledOnMap].Add((killUUID, monsterType));
                                    } catch (Exception e)
                                    {
                                        textBuffer.Append("\tError parsing hl100 report_to_demo in " + info.Key + ": " + e.ToString() + "\n");
                                    }
                                }
                                break;
                            }

                        case Bxt.RuntimeDataType.GAME_END_MARKER:
                            {
                                if (!gameEndReported)
                                {
                                    textBuffer.Append("\tGAME END — Frame: " + i + "\n", Color.ForestGreen);
                                    gameEndReported = true;
                                }
                                datanode.Nodes.Add(new TreeNode("-- GAME END --") { ForeColor = Color.ForestGreen });
                                break;
                            }
                        case Bxt.RuntimeDataType.LOADED_MODULES:
                            {
                                var modulesnode = new TreeNode("Loaded modules [" + ((Bxt.LoadedModules)t.Value).filenames.Count + "]") { ForeColor = Color.LightGreen };
                                modulesnode.Nodes.AddRange(((Bxt.LoadedModules)t.Value).filenames.Select(x => new TreeNode(x) { ForeColor = Color.LightGreen }).ToArray());
                                datanode.Nodes.Add(modulesnode);
                                break;
                            }
                        case Bxt.RuntimeDataType.CUSTOM_TRIGGER_COMMAND:
                            {
                                var trigger = (Bxt.CustomTriggerCommand)t.Value;
                                textBuffer.Append("\t" + $"Custom trigger X1:{trigger.corner_max.X} Y1:{trigger.corner_max.Y} Z1:{trigger.corner_max.Z} X2:{trigger.corner_min.X} Y2:{trigger.corner_min.Y} Z2:{trigger.corner_min.Z}" + " — Frame: " + i + "\n");
                                datanode.Nodes.Add(new TreeNode($"Custom trigger X1:{trigger.corner_max.X} Y1:{trigger.corner_max.Y} Z1:{trigger.corner_max.Z} X2:{trigger.corner_min.X} Y2:{trigger.corner_min.Y} Z2:{trigger.corner_min.Z}")
                                {
                                    ForeColor = Color.Orange,
                                    Nodes = { new TreeNode("Command: " + trigger.command) { ForeColor = Color.Orange } }
                                });
                                break;
                            }
                        case Bxt.RuntimeDataType.EDICTS:
                            {
                                if (((Bxt.Edicts)t.Value).edicts > 900)
                                {
                                    textBuffer.Append("\t" + "Max edicts value is higher than 900: " + ((Bxt.Edicts)t.Value).edicts + "\n", IllegalColor);
                                }
                                datanode.Nodes.Add(new TreeNode("Max edicts: " + ((Bxt.Edicts)t.Value).edicts) { ForeColor = Color.Violet });
                                break;
                            }
                        case Bxt.RuntimeDataType.PLAYERHEALTH:
                            {
                                datanode.Nodes.Add(new TreeNode("Player health: " + ((Bxt.PlayerHealth)t.Value).playerhealth) { ForeColor = Color.LightSalmon });
                                break;
                            }
                        case Bxt.RuntimeDataType.SPLIT_MARKER:
                            {
                                var split = (Bxt.SplitMarker)t.Value;
                                textBuffer.Append("\t" + $"Split trigger X1:{split.corner_max.X} Y1:{split.corner_max.Y} Z1:{split.corner_max.Z} X2:{split.corner_min.X} Y2:{split.corner_min.Y} Z2:{split.corner_min.Z}" + " — Frame: " + i + "\n");
                                datanode.Nodes.Add(new TreeNode($"Split trigger X1:{split.corner_max.X} Y1:{split.corner_max.Y} Z1:{split.corner_max.Z} X2:{split.corner_min.X} Y2:{split.corner_min.Y} Z2:{split.corner_min.Z}")
                                {
                                    ForeColor = Color.Orange,
                                    Nodes = { new TreeNode("Name: " + split.name + " | Map name: " + split.map_name) { ForeColor = Color.Orange } }
                                });
                                break;
                            }
                        case Bxt.RuntimeDataType.FLAGS:
                            {
                                int bxtFlags = ((Bxt.Flags)t.Value).flags;
                                bool bigMap = (bxtFlags & 1) != 0;
                                if (bigMap)
                                {
                                    textBuffer.Append("\tThis runner has used bxt_enable_big_map and didn't restart the game before the run. This command is not intended for RTA leaderboard runs.\n", IllegalColor);
                                }
                                datanode.Nodes.Add(new TreeNode("BXT Flags: " + bxtFlags) { ForeColor = Color.LightSalmon });
                                break;
                            }
                        default:
                            {
                                datanode.Nodes.Add(new TreeNode("Invalid bxt data!") { ForeColor = Color.Red });
                                break;
                            }
                    }
                }
                demonode.Nodes.Add(datanode);
            }
            BXTTreeView.Nodes.Add(demonode);
        }

        private void Verification_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Verification_DragDrop(object sender, DragEventArgs e)
        {
            var dropfiles = (string[]) e.Data.GetData(DataFormats.FileDrop);
            Verify(dropfiles);
            e.Effect = DragDropEffects.None;
        }

        private void clearDemosButton_Click(object sender, EventArgs e)
        {
            mrtb.Text = "Deleting bxt tree nodes, this may take a while...\n";
            DemopathList.Clear();
            BXTTreeView.BeginUpdate();
            BXTTreeView.Nodes.Clear();
            BXTTreeView.EndUpdate();
            mrtb.Text = "Demos cleared, ready to parse\n";
        }

        private void toggleModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isScriptlessMode = !isScriptlessMode;
            toggleModeToolStripMenuItem.Text = isScriptlessMode ? "Scriptless" : "Scripted";
            this.Text = isScriptlessMode ? "Verification Scriptless" : "Verification Scripted";
        }
    }
}
