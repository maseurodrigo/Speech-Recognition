using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using System.Windows.Forms;
using Speech_Recognition.Constructors;

namespace Speech_Recognition
{
    public partial class speechRecogForm : Form
    {
        // Main app. default variables
        public static SpeechSynthesizer currentSpeechBot { get; private set; }
        public static Dictionary<String, bool> botDataStatus { get; private set; }

        public speechRecogForm() { InitializeComponent(); }

        private void speechRecogForm_Load(object sender, EventArgs e) {
            // Instance a new Travis class
            Travis initTravis = new Travis(this, new CultureInfo("en-US"), @"Resources/BaseDataSchema.json", @"Resources/BaseData.json", 80, 0.75);
            currentSpeechBot = initTravis._Travis;
            botDataStatus = initTravis.botDataStatus;
        }
    }
}