// Responsible: TEAM (cn569321)
// Copyright:   Copyright 2016 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Reflection;

# region Keysight Libraries
using Ivi.Driver.Interop;

using Keysight.Kmf.Gui.Trace;
using Keysight.Kmf.ComponentModel;
using Keysight.Kmf.Dsp;
using Keysight.Kmf.Hardware.Measurements;
using Keysight.Kmf.Dsp.Spectrum;

using Agilent.AgMD2.Interop;
//using Agilent.AgM9393.Interop;
#endregion

#region Plugin Libraries
using Keysight.S8901A.Common;
using Keysight.S8901A.Measurement;
using LinqToStdf;
using LinqToStdf.Records.V4;
using Keysight.S8901A.Measurement.TapInstruments;
using Keysight.S8901A.Common.MeasurementDataCommunication;
using Keysight.S8901A.Measurement.KmfResultView;
#endregion

using Keysight.Tap;  // Use Platform infrastructure/core components (log,TestStep definition, etc)
//using Keysight.KtM9420.Interop;

namespace Keysight.S8901A.Measurement.TapSteps
{

    public abstract class PA_TestStep : PACommonTestStep
    {

        //#region Settings
        //[Display(Name: "PA Instrument", Order: -20000)]
        //public PA_Instrument GetInstrument() { get; set; }

        //#endregion

        public PA_TestStep()
        {
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();

            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
            
            // If the current running test is the first test step of the plan
            // we think is the beginning of the test plan .
            if (TestPlanRunningHelper.GetInstance().FirstTestStep == this.Name)
            {
                TestPlanRunningHelper.GetInstance().OnTestPlanStarted();
            }
        }

        public override void Run()
        {
            // ToDo: Add test case code here
            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            // UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

        public void Logging(LogLevel lvl, string msg, params object[] args)
        {
            PlugInBase b = GetInstrument().Measurement as PlugInBase;
            b.Logging(lvl, msg, args);
        }

        public Action<bool, string, double, ORFSResult, ResultSource>
       PublishResult_Orfs = (meas, tech, freq, orfsResult, result) =>
       {
           string item_name = string.Empty;
           string offset = string.Empty;
           string rbw = string.Empty;
           if (meas)
           {
               PA_LOG(PA_LOG_TRS, "ORFS", "[{0}], {1}, Freq:{2,5}MHz, Pout:{3, 4:F2}dBm", orfsResult.result, tech, freq, orfsResult.measPout);


               if ((orfsResult.meastype == "Modulation") ||
                   (orfsResult.meastype == "ModulationAndSwitching"))
               {
                   for (int i = 0; i < orfsResult.mOffsetResult.Count(); i++)
                   {
                       if (orfsResult.mOffsetResult[i].enabled)
                       {
                           if (orfsResult.mOffsetResult[i].frequency >= 1e6)
                               offset = string.Format("{0}M", orfsResult.mOffsetResult[i].frequency / 1e6);
                           else
                               offset = string.Format("{0}K", orfsResult.mOffsetResult[i].frequency / 1e3);

                           if (orfsResult.mOffsetResult[i].resolutionBW >= 1e6)
                               rbw = (orfsResult.mOffsetResult[i].resolutionBW / 1e6).ToString() + "MHz";
                           else
                               rbw = (orfsResult.mOffsetResult[i].resolutionBW / 1e3).ToString() + " KHz";

                           item_name = string.Format("ORFS_M_{0}_L", offset);

                           result.Publish(item_name, new List<string> { "Technology", "Frequency(MHz)", "Pout(dBm)", "Ref Power(dBm)", "RBW", "Rel(dBm)", "Delta2Lim(dB)", "Abs(dBm)", "Rel Lim(dB)", "Abs Lim(dBm)", "Lim Type" },
                                           tech, freq, orfsResult.measPout,
                                           orfsResult.mRefPower, rbw,
                                           orfsResult.mOffsetResult[i].lowerRel,
                                           orfsResult.mOffsetResult[i].lowerDelta,
                                           orfsResult.mOffsetResult[i].lowerAbs,
                                           orfsResult.mOffsetResult[i].lowerRelLim,
                                           orfsResult.mOffsetResult[i].lowerAbsLim,
                                           orfsResult.mOffsetResult[i].limitType.ToString());

                           PA_LOG(PA_LOG_TRD, item_name, "Ref Power{0, 4:F2}dBm, RBW:{1}, Rel;{2, 4:F2}dBm, Delta2Lim:{3, 4:F2}dB, Abs: {4:4:F2}dBm, RelLim: {5, 4:F2}dB, AbsLim: {6, 4:F2}dBm, LimType:{7}",
                                                       orfsResult.mRefPower,
                                                       rbw, orfsResult.mOffsetResult[i].lowerRel,
                                                       orfsResult.mOffsetResult[i].lowerDelta,
                                                       orfsResult.mOffsetResult[i].lowerAbs,
                                                       orfsResult.mOffsetResult[i].lowerRelLim,
                                                       orfsResult.mOffsetResult[i].lowerAbsLim,
                                                       orfsResult.mOffsetResult[i].limitType.ToString());


                           item_name = string.Format("ORFS_M_{0}_H", offset);

                           result.Publish(item_name, new List<string> { "Technology", "Frequency(MHz)", "Pout(dBm)", "Ref Power(dBm)", "RBW", "Rel(dBm)", "Delta2Lim(dB)", "Abs(dBm)", "Rel Lim(dB)", "Abs Lim(dBm)", "Lim Type" },
                                           tech, freq, orfsResult.measPout,
                                           orfsResult.mRefPower, rbw,
                                           orfsResult.mOffsetResult[i].upperRel,
                                           orfsResult.mOffsetResult[i].upperDelta,
                                           orfsResult.mOffsetResult[i].upperAbs,
                                           orfsResult.mOffsetResult[i].upperRelLim,
                                           orfsResult.mOffsetResult[i].upperAbsLim,
                                           orfsResult.mOffsetResult[i].limitType.ToString());

                           PA_LOG(PA_LOG_TRD, item_name, "Ref Power{0, 4:F2}dBm, RBW:{1}, Rel;{2, 4:F2}dBm, Delta2Lim:{3, 4:F2}dB, Abs: {4:4:F2}dBm, RelLim: {5, 4:F2}dB, AbsLim: {6, 4:F2}dBm, LimType:{7}",
                                                       orfsResult.mRefPower,
                                                       rbw, orfsResult.mOffsetResult[i].upperRel,
                                                       orfsResult.mOffsetResult[i].upperDelta,
                                                       orfsResult.mOffsetResult[i].upperAbs,
                                                       orfsResult.mOffsetResult[i].upperRelLim,
                                                       orfsResult.mOffsetResult[i].upperAbsLim,
                                                       orfsResult.mOffsetResult[i].limitType.ToString());
                       }
                   }
               }

               if ((orfsResult.meastype == "Switching") ||
                  (orfsResult.meastype == "ModulationAndSwitching"))
               {
                   for (int i = 0; i < orfsResult.sOffsetResult.Count(); i++)
                   {
                       if (orfsResult.sOffsetResult[i].enabled)
                       {
                           if (orfsResult.sOffsetResult[i].frequency >= 1e6)
                               offset = string.Format("{0}M", orfsResult.sOffsetResult[i].frequency / 1e6);
                           else
                               offset = string.Format("{0}K", orfsResult.sOffsetResult[i].frequency / 1e3);

                           if (orfsResult.sOffsetResult[i].resolutionBW >= 1e6)
                               rbw = (orfsResult.sOffsetResult[i].resolutionBW / 1e6).ToString() + "MHz";
                           else
                               rbw = (orfsResult.sOffsetResult[i].resolutionBW / 1e3).ToString() + " KHz";

                           item_name = string.Format("ORFS_S_{0}_L", offset);

                           result.Publish(item_name, new List<string> { "Technology", "Frequency(MHz)", "Pout(dBm)", "Ref Power(dBm)", "RBW", "Rel(dBm)", "Delta2Lim(dB)", "Abs(dBm)", "Rel Lim(dB)", "Abs Lim(dBm)", "Lim Type" },
                                           tech, freq, orfsResult.measPout,
                                           orfsResult.sRefPower, rbw,
                                           orfsResult.sOffsetResult[i].lowerRel,
                                           orfsResult.sOffsetResult[i].lowerDelta,
                                           orfsResult.sOffsetResult[i].lowerAbs,
                                           orfsResult.sOffsetResult[i].lowerRelLim,
                                           orfsResult.sOffsetResult[i].lowerAbsLim,
                                           orfsResult.sOffsetResult[i].limitType.ToString());

                           PA_LOG(PA_LOG_TRD, item_name, "Ref Power{0, 4:F2}dBm, RBW:{1}, Rel;{2, 4:F2}dBm, Delta2Lim:{3, 4:F2}dB, Abs: {4:4:F2}dBm, RelLim: {5, 4:F2}dB, AbsLim: {6, 4:F2}dBm, LimType:{7}",
                                                       orfsResult.sRefPower,
                                                       rbw, orfsResult.sOffsetResult[i].lowerRel,
                                                       orfsResult.sOffsetResult[i].lowerDelta,
                                                       orfsResult.sOffsetResult[i].lowerAbs,
                                                       orfsResult.sOffsetResult[i].lowerRelLim,
                                                       orfsResult.sOffsetResult[i].lowerAbsLim,
                                                       orfsResult.sOffsetResult[i].limitType.ToString());


                           item_name = string.Format("ORFS_S_{0}_H", offset);

                           result.Publish(item_name, new List<string> { "Technology", "Frequency(MHz)", "Pout(dBm)", "Ref Power(dBm)", "RBW", "Rel(dBm)", "Delta2Lim(dB)", "Abs(dBm)", "Rel Lim(dB)", "Abs Lim(dBm)", "Lim Type" },
                                           tech, freq, orfsResult.measPout,
                                           orfsResult.sRefPower, rbw,
                                           orfsResult.sOffsetResult[i].upperRel,
                                           orfsResult.sOffsetResult[i].upperDelta,
                                           orfsResult.sOffsetResult[i].upperAbs,
                                           orfsResult.sOffsetResult[i].upperRelLim,
                                           orfsResult.sOffsetResult[i].upperAbsLim,
                                           orfsResult.sOffsetResult[i].limitType.ToString());

                           PA_LOG(PA_LOG_TRD, item_name, "Ref Power{0, 4:F2}dBm, RBW:{1}, Rel;{2, 4:F2}dBm, Delta2Lim:{3, 4:F2}dB, Abs: {4:4:F2}dBm, RelLim: {5, 4:F2}dB, AbsLim: {6, 4:F2}dBm, LimType:{7}",
                                                       orfsResult.sRefPower,
                                                       rbw, orfsResult.sOffsetResult[i].upperRel,
                                                       orfsResult.sOffsetResult[i].upperDelta,
                                                       orfsResult.sOffsetResult[i].upperAbs,
                                                       orfsResult.sOffsetResult[i].upperRelLim,
                                                       orfsResult.sOffsetResult[i].upperAbsLim,
                                                       orfsResult.sOffsetResult[i].limitType.ToString());
                       }

                   }
               }
           }
       };

        public PA_Instrument GetInstrument()
        {
            if (pa_instrument.GetType() == typeof(PA_Instrument))
            {
                return pa_instrument as PA_Instrument;
            }
            else
            {
                Log.Error("Please choose correct PA instrument, it should be a kind of PA Instrument");
                throw (new Exception("Please choose correct PA instrument"));

            }
        }
    }

    #region Test Condition Setup

    [Display(Name: "Test Condition Setup", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("Test Condition Setup")]
    [AllowAnyChild]
    public class TestCondition_Setup : PA_TestStep
    {
        public TestCondition_Setup()
        {
            TestStepList tl = new TestStepList();
            TestStep t;

            t = (TestStep)new SelectTechnology();
            tl.Add(t);

            t = (TestStep)new Source_Analyzer_Setup();
            tl.Add(t);

            t = (TestStep)new CableLoss_Setup();
            tl.Add(t);

            t = (TestStep)new DCSMU_Setup();
            tl.Add(t);

            t = (TestStep)new DIO_Setup();
            tl.Add(t);

            //t = (TestStep)new VNA_Setup();
            //tl.Add(t);

            //t = (TestStep)new Switch_Setup();
            //tl.Add(t);

            this.ChildTestSteps = tl;

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Select Technology", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("Select the Technology defined in INI File")]
    [Browsable(true)]
    [TcfVisible]
    public class SelectTechnology : PA_TestStep
    {
        #region Settings

        #region Direction
        private DIRECTION _dir;
        [Display(Name: "Direction", Order: 0.1)]
        public DIRECTION dir
        {
            get { return _dir; }
            set
            {
                _dir = value;
                // Initialize a null PlugInMeas to fetch the technologies list
                if ((GetInstrument() != null) && (technology == null || technology == string.Empty))
                {
                    _meas = new PlugInMeas(null, null, null, null, null);
                    bool ret = _meas.TestConditionSetup_LoadTestParameters(GetInstrument().config_file, GetInstrument().waveform_path);
                }
                technologies = _meas.TestConditionSetup_GetLoadedTechnologies(_dir);

                switch(_dir)
                {
                    default:
                    case DIRECTION.UPLINK:
                        technology = technologies[_uplink_index];
                        break;
                    case DIRECTION.DOWNLINK:
                        technology = technologies[_downlink_index];
                        break;
                }
            }
        }
        #endregion

        #region Technology

        private string _technology;
        private int _uplink_index = 0;
        private int _downlink_index = 0;
        [Display(Name: "Technology", Order: 1)]
        [AvailableValues("technologies")]
        [TcfVisible]
        public string technology
        {
            get { return _technology; }
            set
            {
                _technology = value;

                #region Save the current Technology Selection to the index
                if (dir == DIRECTION.UPLINK)
                {
                    for (int i = 0; i < technologies.Count(); i++)
                        if (technologies[i].Equals(_technology))
                        {
                            _uplink_index = i;
                            break;
                        }
                }
                else
                {
                    for (int i = 0; i < technologies.Count(); i++)
                        if (technologies[i].Equals(_technology))
                        {
                            _downlink_index = i;
                            break;
                        }
                }
                #endregion

                SUPPORTEDFORMAT tf = _meas.TestConditionSetup_GetFormat(_technology);
                DIRECTION d = _meas.TestConditionSetup_GetDirection(_technology);

                if (tf == SUPPORTEDFORMAT.CW)
                {
                    tech_is_cw = true;
                    is_dynamicEVM = false;
                }
                else
                {
                    tech_is_cw = false;
                }

                if (
                      ( (d == DIRECTION.UPLINK) &&
                        (tf == SUPPORTEDFORMAT.WLAN11A
                        || tf == SUPPORTEDFORMAT.WLAN11AC
                        || tf == SUPPORTEDFORMAT.WLAN11AX
                        || tf == SUPPORTEDFORMAT.WLAN11B
                        || tf == SUPPORTEDFORMAT.WLAN11G
                        || tf == SUPPORTEDFORMAT.WLAN11J
                        || tf == SUPPORTEDFORMAT.WLAN11N
                        || tf == SUPPORTEDFORMAT.WLAN11P) )
                      ||
                      ( (d == DIRECTION.DOWNLINK) && (tf == SUPPORTEDFORMAT.LTETDD) )
                    )
                {
                    allow_dynamic_operation = true;
                }
                else
                {
                    allow_dynamic_operation = false;
                    is_dynamicEVM = false;
                }

                if (tf == SUPPORTEDFORMAT.GSM)
                {
                    tech_is_gsm = true;
                }
                else
                {
                    tech_is_gsm = false;
                    is_PolarPA = false;
                }
            }
        }

        #endregion

        #region Not Browsable
        private PlugInMeas _meas;

        [Browsable(false)]
        public bool tech_is_cw { get; set; }

        [Browsable(false)]
        public bool allow_dynamic_operation { get; set; }

        [Browsable(false)]
        public bool tech_is_gsm { get; set; }

        [Browsable(false)]
        public bool seq_or_dynamic_op_at_runtime { get; set; }

        [Browsable(false)]
        public List<string> technologies { get; set; }

        [Browsable(false)]
        public List<string> awg_waveforms { get; set; }

        private void check_status(bool seq, bool dyn, bool user_defined)
        {
            if (seq)
            {
                if (!dyn)
                    seq_or_dynamic_op_at_runtime = true;
                else if (user_defined)
                    seq_or_dynamic_op_at_runtime = true;
                else
                    seq_or_dynamic_op_at_runtime = false;
            }
            else
                seq_or_dynamic_op_at_runtime = false;
        }

        #endregion

        [Display(Name: "CW Waveform", Order: 2)]
        [EnabledIf("tech_is_cw", true, HideIfDisabled = true)]
        [TcfVisible]
        public CW_WAVEFORM_TYPE cw_waveform { get; set; }

        private bool _is_seq;
        [Display(Name: "Use Sequence", Order: 3)]
        [TcfVisible]
        public bool is_seq
        {
            get
            {
                return _is_seq;
            }
            set
            {
                _is_seq = value;
                if (!_is_seq)
                {
                    duty_cycle = 1;
                    is_dynamicEVM = false;
                }

                check_status(_is_seq, is_dynamicEVM,
                    awg_waveform_gen_method == AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME);
            }
        }

        private bool _is_dynamicEVM;
        [Display(Name: "Use Dynamic Operation", Order: 4)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("allow_dynamic_operation", true, HideIfDisabled = true)]
        [TcfVisible]
        public bool is_dynamicEVM
        {
            get { return _is_dynamicEVM; }
            set
            {
                _is_dynamicEVM = value;

                check_status(is_seq, _is_dynamicEVM,
                    awg_waveform_gen_method == AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME);
            }
        }

        public enum AWG_WAVEFORM_GEN_METHOD
        {
            PLAY_WAVEFORM_PRELOADED,
            GEN_AND_PLAY_WAVEFORM_AT_RUNTIME
        }

        private AWG_WAVEFORM_GEN_METHOD _awg_waveform_gen_method;
        [Display(Name: "AWG Waveform Play Method", Order: 5)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        public AWG_WAVEFORM_GEN_METHOD awg_waveform_gen_method
        {
            get { return _awg_waveform_gen_method; }
            set
            {
                _awg_waveform_gen_method = value;

                check_status(is_seq, is_dynamicEVM,
                    _awg_waveform_gen_method == AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME);
            }
        }

        [Display(Name: "AWG Waveform Pre-loaded", Order: 6)]
        [AvailableValues("awg_waveforms")]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        [EnabledIf("awg_waveform_gen_method", AWG_WAVEFORM_GEN_METHOD.PLAY_WAVEFORM_PRELOADED, HideIfDisabled = true)]
        public string awg_waveform { get; set; }

        private double _duty_cycle;
        [Display(Name: "Duty Cycle", Group: "Arb Sequence", Order: 10)]
        [EnabledIf("seq_or_dynamic_op_at_runtime", true, HideIfDisabled = true)]
        [TcfVisible]
        public double duty_cycle
        {
            get
            {
                return _duty_cycle;
            }
            set
            {
                _duty_cycle = value;
                if (_duty_cycle > 0.99)
                {
                    _duty_cycle = 0.99;
                }
                else if (_duty_cycle < 0.01)
                {
                    _duty_cycle = 0.01;
                }
            }
        }

        [Display(Name: "Lead Time", Group: "Arb Sequence", Order: 11)]
        [EnabledIf("seq_or_dynamic_op_at_runtime", true, HideIfDisabled = true)]
        [Unit("s")]
        [TcfVisible]
        public double lead_time { get; set; }

        [Display(Name: "Lag Time", Group: "Arb Sequence", Order: 12)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        [Unit("s")]
        [EnabledIf("awg_waveform_gen_method", AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME, HideIfDisabled = true)]
        [TcfVisible]
        public double lag_time { get; set; }

        //the trigger delay is the integer times of 100ns
        private double _trigger_delay;
        [Display(Name: "Trigger Delay", Group: "Arb Sequence", Order: 13)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        [Unit("s")]
        [EnabledIf("awg_waveform_gen_method", AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME, HideIfDisabled = true)]
        [TcfVisible]
        public double trigger_delay
        {
            get
            {
                return _trigger_delay;
            }
            set
            {
                _trigger_delay = (int)(value * 10.0e6);
                _trigger_delay = _trigger_delay / 10.0e6;
            }
        }

        private double _PAEnableVoltage;
        [Display(Name: "PA Enable Voltage", Group: "Arb Sequence", Order: 14)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        [Unit("V")]
        [EnabledIf("awg_waveform_gen_method", AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME, HideIfDisabled = true)]
        public double PAEnableVoltage
        {
            get
            {
                return _PAEnableVoltage;
            }
            set
            {
                _PAEnableVoltage = value;
                if (_PAEnableVoltage >= 3.4)
                {
                    _PAEnableVoltage = 3.4;
                }
            }
        }

        #region Dynamic EVM Timing
        [Display(Name: "Dynamic Operation Timing", Order: 15)]
        [Browsable(true)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        public void Import()
        {
            // How to dispose the previous one when closing?
            if (_DEVMTimingGui != null)
            {
                _DEVMTimingGui.Close();
                _DEVMTimingGui = null;
            }

            _DEVMTimingGui = new DEVMTimingGui();
            _DEVMTimingGui.Show();
        }

        // Create the GUI and KMF display controls
        private DEVMTimingGui _DEVMTimingGui;
        #endregion

        #region polar PA
        private bool _is_Polar_PA;
        [Display(Name: "Use Polar PA", Order: 5)]
        [EnabledIf("tech_is_gsm", true, HideIfDisabled = true)]
        public bool is_PolarPA
        {
            get
            {
                return _is_Polar_PA;
            }
            set
            {
                _is_Polar_PA = value;
                if (_is_Polar_PA)
                    VrampVoltage = 1.2;
            }
        }

        private double _VrampVoltage;
        [Display(Name: "Vramp Voltage", Group: "Vramp", Order: 14)]
        [Unit("V")]
        [EnabledIf("is_PolarPA", true, HideIfDisabled = true)]
        public double VrampVoltage
        {
            get
            {
                return _VrampVoltage;
            }
            set
            {
                _VrampVoltage = value;
                if (_VrampVoltage >= 1.8)
                {
                    _VrampVoltage = 1.8;
                }
            }
        }
        #endregion

        #endregion

        public SelectTechnology()
        {
            // Initialize a null PlugInMeas to fetch the technologies list
            if ((GetInstrument() != null) && (technology == null || technology == string.Empty))
            {
                _meas = new PlugInMeas(null, null, null, null, null);
                bool ret = _meas.TestConditionSetup_LoadTestParameters(GetInstrument().config_file, GetInstrument().waveform_path);
                technologies = _meas.TestConditionSetup_GetLoadedTechnologies(dir);
            }

            awg_waveforms = new List<string>();
            if ((GetInstrument() != null) && File.Exists(GetInstrument().awg_pulse_config_file))
            {
                GetInstrument().ParseAwgPulseConfigFile(GetInstrument().awg_pulse_config_file);
                foreach (var i in GetInstrument().awgPulseConfig)
                {
                    awg_waveforms.Add(i.awgArbDisplayName);
                }
            }

            is_seq = false;
            is_dynamicEVM = false;
            awg_waveform_gen_method = AWG_WAVEFORM_GEN_METHOD.GEN_AND_PLAY_WAVEFORM_AT_RUNTIME;

            duty_cycle = 1.0;
            lead_time = 1.0e-6;
            lag_time = 1.0e-6;
            trigger_delay = 10.0e-6;

            is_PolarPA = false;
            VrampVoltage = 1.2;
            PAEnableVoltage = 1.0;

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            string selected_waveform = null;
            try
            {
                technologies = GetInstrument().Measurement.TestConditionSetup_GetLoadedTechnologies(dir);

                int triggerLineNumber = (int)GetInstrument().chassis_trigger_line;

                PlugInMeas meas = GetInstrument().Measurement;
                bool awgPreloaded = awg_waveform_gen_method == AWG_WAVEFORM_GEN_METHOD.PLAY_WAVEFORM_PRELOADED ? true : false;

                double lead_time_used;
                double lag_time_used;
                double duty_cycle_used;
                double voltage_used;
                double trigger_delay_used;
                string awg_arb = null;

                if (awgPreloaded)
                {
                    var a = from i in GetInstrument().awgPulseConfig
                            where i.awgArbDisplayName.Equals(awg_waveform)
                            select i;
                    AwgPulseConfig ac = a.FirstOrDefault();
                    lead_time_used = ac.leadTime;
                    lag_time_used = ac.lagTime;
                    duty_cycle_used = ac.dutyCycle;
                    voltage_used = ac.pulseVoltage;
                    trigger_delay_used = ac.trigDelay;
                    awg_arb = ac.awgArbName;
                }
                else
                {
                    lead_time_used = lead_time;
                    lag_time_used = lag_time;
                    duty_cycle_used = duty_cycle;
                    voltage_used = PAEnableVoltage;
                    trigger_delay_used = trigger_delay;
                }

                ret = meas.TestConditionSetup_SelectTechnology(
                    ref selected_waveform, technology, ref GetInstrument().iAwgVrampLoadedNumber,
                    ref GetInstrument().bVrampMaskLoaded, ref GetInstrument().VrampMask,
                    is_dynamicEVM, is_seq,
                    cw_waveform, lead_time_used,
                    lag_time_used, duty_cycle_used,
                    trigger_delay_used, voltage_used,
                    is_PolarPA, VrampVoltage, triggerLineNumber,
                    awgPreloaded,
                    awg_arb,
                    GetInstrument().use_secondary_source_analyzer,
                    GetInstrument().secondary_source_analyzer_config);

                GetInstrument().selected_technology = technology;
                if (GetInstrument().selected_waveform == selected_waveform)
                    GetInstrument().waveform_changed = false;
                else
                {
                    GetInstrument().waveform_changed = true;
                    GetInstrument().reference_generated = false;
                }
                GetInstrument().selected_waveform = selected_waveform;
                GetInstrument().dDutycycle = duty_cycle_used;
                GetInstrument().dLeadTime = lead_time_used;
                GetInstrument().dLagTime = lag_time_used;
                GetInstrument().dRFTriggerDelay = trigger_delay_used;// wlan_evm_delay;

                PA_LOG(PA_LOG_TC, "TECH ", "[{2}] Select Technology {0}, Waveform {1}!", technology, GetInstrument().selected_waveform, ret ? "Pass" : "Fail");

                TestPlanRunningHelper.GetInstance().IsDPDActive = false;
                TestPlanRunningHelper.GetInstance().IsEtActive = false;

            }
            catch (Exception ex)
            {
                ret = false;
            }

            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            if (ret)
                UpgradeVerdict(Verdict.Pass);
            else
                UpgradeVerdict(Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    /*
    [Display(Name: "Select Technology 2", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("Select the Technology defined in INI File")]
    [Browsable(false)]
    public class SelectTechnology2 : PA_TestStep
    {
        public enum TECH_SELECTION_METHOD
        {
            SELECT_FROM_LIST = 0,
            SELECT_MANUALLY = 1
        }

        #region Settings

        #region Format/Standard/Bandwidth => Technology
        private TECH_SELECTION_METHOD _tech_select_method;
        [Browsable(false)]
        [Display(Name: "Technology Select Method", Order: 0.9)]
        public TECH_SELECTION_METHOD tech_select_method
        {
            get { return _tech_select_method; }
            set
            {
                _tech_select_method = value;
                if (_tech_select_method == TECH_SELECTION_METHOD.SELECT_MANUALLY
                    && format != null && format.Equals("WLAN"))
                {
                    ShowWlanStandard(true);
                    BuildWlanBandwidthList();
                }
                else if (_tech_select_method == TECH_SELECTION_METHOD.SELECT_MANUALLY
                    && format != null && format.Equals("LTE FDD"))
                {
                    ShowWlanStandard(false);
                    BuildLteFddBandwidthList();
                }
                else if (_tech_select_method == TECH_SELECTION_METHOD.SELECT_MANUALLY
                    && format != null && format.Equals("LTE TDD"))
                {
                    ShowWlanStandard(false);
                    BuildLteTddBandwidthList();
                }
                else
                {
                    ShowWlanStandard(false);
                    show_bandwidth = false;
                }
            }
        }

        private string _technology;
        [Display(Name: "Technology", Order: 1)]
        [AvailableValues("technologies")]
        [EnabledIf("tech_select_method", TECH_SELECTION_METHOD.SELECT_FROM_LIST)]
        public string technology
        {
            get { return _technology; }
            set
            {
                _technology = value;

                if (_technology.Equals("CW"))
                {
                    tech_is_cw = true;
                    is_dynamicEVM = false;
                }
                else
                {
                    tech_is_cw = false;
                }

                if (_technology.Contains("WLAN"))
                {
                    tech_is_wlan = true;
                }
                else
                {
                    tech_is_wlan = false;
                    is_dynamicEVM = false;
                }

                if (_technology.Contains("GSM"))
                {
                    tech_is_gsm = true;
                }
                else
                {
                    tech_is_gsm = false;
                    is_PolarPA = false;
                }
            }
        }

        private string _format;
        [Display(Name: "Format", Order: 1.1)]
        [AvailableValues("formats")]
        [EnabledIf("tech_select_method", TECH_SELECTION_METHOD.SELECT_MANUALLY, HideIfDisabled = true)]
        public string format
        {
            get { return _format; }
            set
            {
                _format = value;
                if (_format.Equals("LTE FDD"))
                {
                    ShowWlanStandard(false);
                    BuildLteFddBandwidthList();
                    tech_is_cw = false;
                    tech_is_wlan = false;
                    is_dynamicEVM = false;
                }
                else if (_format.Equals("LTE TDD"))
                {
                    ShowWlanStandard(false);
                    BuildLteTddBandwidthList();
                    tech_is_cw = false;
                    tech_is_wlan = false;
                    is_dynamicEVM = false;
                }
                else if (_format.Equals("WLAN"))
                {
                    ShowWlanStandard(true);
                    BuildWlanBandwidthList();
                    tech_is_wlan = true;
                    tech_is_cw = false;
                }
                else if (_format.Equals("CW"))
                {
                    ShowWlanStandard(false);
                    show_bandwidth = false;
                    tech_is_cw = true;
                    tech_is_wlan = false;
                    is_dynamicEVM = false;
                }
                else
                {
                    ShowWlanStandard(false);
                    show_bandwidth = false;
                    tech_is_cw = false;
                    tech_is_wlan = false;
                }

                UpdateTechnology();
            }
        }

        private string _wlan_standard;
        [Display(Name: "WLAN Standard", Order: 1.2)]
        [AvailableValues("wlan_standards")]
        [EnabledIf("show_wlan_standard", true, HideIfDisabled = true)]
        public string wlan_standard
        {
            get { return _wlan_standard; }
            set
            {
                _wlan_standard = value;
                BuildWlanBandwidthList();

                UpdateTechnology();
            }
        }

        private string _bandwidth;
        [Display(Name: "BandWidth", Order: 1.3)]
        [AvailableValues("bandwidths")]
        [EnabledIf("show_bandwidth", true, HideIfDisabled = true)]
        public string bandwidth
        {
            get { return _bandwidth; }
            set
            {
                _bandwidth = value;
                UpdateTechnology();
            }
        }
        #endregion

        #region Not Browsable
        [Browsable(false)]
        public bool tech_is_cw { get; set; }

        [Browsable(false)]
        public bool tech_is_wlan { get; set; }

        [Browsable(false)]
        public bool tech_is_gsm { get; set; }

        [Browsable(false)]
        public bool show_wlan_standard { get; set; }

        [Browsable(false)]
        public bool show_bandwidth { get; set; }

        [Browsable(false)]
        public bool tech_is_lte_fdd { get; set; }

        [Browsable(false)]
        public bool tech_is_lte_tdd { get; set; }

        [Browsable(false)]
        public List<string> technologies { get; set; }

        [Browsable(false)]
        public List<string> formats { get; set; }

        [Browsable(false)]
        public List<string> wlan_standards { get; set; }

        [Browsable(false)]
        public List<string> bandwidths { get; set; }

        #endregion

        [Display(Name: "CW Waveform", Order: 2)]
        [EnabledIf("tech_is_cw", true, HideIfDisabled = true)]
        public CW_WAVEFORM_TYPE cw_waveform { get; set; }

        private bool _is_seq;
        [Display(Name: "Is Sequence", Order: 3)]
        public bool is_seq
        {
            get
            {
                return _is_seq;
            }
            set
            {
                _is_seq = value;
                if (!_is_seq)
                    duty_cycle = 1;
            }
        }

        private bool _is_dynamicEVM;
        [Display(Name: "Is Dynamic Operation", Order: 4)]
        [EnabledIf("tech_is_wlan", true, HideIfDisabled = true)]
        public bool is_dynamicEVM
        {
            get
            {
                return _is_dynamicEVM;
            }
            set
            {
                _is_dynamicEVM = value;
                if (_is_dynamicEVM)
                {
                    //duty_cycle = 0.5;
                    //PAEnableVoltage = 1.0;
                }
            }
        }

        private double _duty_cycle;
        [Display(Name: "Duty Cycle", Group: "Arb Sequence", Order: 10)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        public double duty_cycle
        {
            get
            {
                return _duty_cycle;
            }
            set
            {
                _duty_cycle = value;
                if (_duty_cycle > 0.99)
                {
                    _duty_cycle = 0.99;
                }
                else if (_duty_cycle < 0.01)
                {
                    _duty_cycle = 0.01;
                }
            }
         }

        [Display(Name: "Lead Time", Group: "Arb Sequence", Order: 11)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [Unit("s")]
        public double lead_time { get; set; }

        [Display(Name: "Lag Time", Group: "Arb Sequence", Order: 12)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("tech_is_wlan", true, HideIfDisabled = true)]
        [Unit("s")]
        public double lag_time { get; set; }

        //the trigger delay is the integer times of 100ns
        private double _trigger_delay;
        [Display(Name: "Trigger Delay", Group: "Arb Sequence", Order: 13)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("tech_is_wlan", true, HideIfDisabled = true)]
        [Unit("s")]
        public double trigger_delay
        {
            get
            {
                return _trigger_delay;
            }
            set
            {
                _trigger_delay = (int)(value * 10.0e6);
                _trigger_delay = _trigger_delay / 10.0e6;
            }
        }

        private double _PAEnableVoltage;
        [Display(Name: "PA Enable Voltage", Group: "Arb Sequence", Order: 14)]
        [EnabledIf("is_seq", true, HideIfDisabled = true)]
        [EnabledIf("tech_is_wlan", true, HideIfDisabled = true)]
        [Unit("V")]
        public double PAEnableVoltage
        {
            get
            {
                return _PAEnableVoltage;
            }
            set
            {
                _PAEnableVoltage = value;
                if (_PAEnableVoltage >= 3.4)
                {
                    _PAEnableVoltage = 3.4;
                }
            }
        }

        #region Dynamic EVM Timing
        [Display(Name: "Dynamic Operation Timing", Order: 15)]
        [Browsable(true)]
        [EnabledIf("is_dynamicEVM", true, HideIfDisabled = true)]
        public void Import()
        {
            // How to dispose the previous one when closing?
            if (_DEVMTimingGui != null)
            {
                _DEVMTimingGui.Close();
                _DEVMTimingGui = null;
            }

            _DEVMTimingGui = new DEVMTimingGui();
            _DEVMTimingGui.Show();
        }

        // Create the GUI and KMF display controls
        private DEVMTimingGui _DEVMTimingGui;
        #endregion

        #region polar PA
        private bool _is_Polar_PA;
        [Display(Name: "Is Polar PA", Order: 5)]
        [EnabledIf("tech_is_gsm", true, HideIfDisabled = true)]
        public bool is_PolarPA
        {
            get
            {
                return _is_Polar_PA;
            }
            set
            {
                _is_Polar_PA = value;
                if (_is_Polar_PA)
                    VrampVoltage = 1.2;
            }
        }

        private double _VrampVoltage;
        [Display(Name: "Vramp Voltage", Group: "Vramp", Order: 14)]
        [EnabledIf("is_PolarPA", true, HideIfDisabled = true)]
        public double VrampVoltage
        {
            get
            {
                return _VrampVoltage;
            }
            set
            {
                _VrampVoltage = value;
                if (_VrampVoltage >= 1.8)
                {
                    _VrampVoltage = 1.8;
                }
            }
        }
        #endregion

        #endregion

        private void ShowWlanStandard(bool b)
        {
            show_wlan_standard = b;

            if (b)
            {
                bool ac_exist = false;
                bool ax_exist = false;
                bool ay_exist = false;
                bool a_exist = false;
                bool b_exist = false;
                bool g_exist = false;
                bool j_exist = false;
                bool n_exist = false;
                bool p_exist = false;

                IEnumerable<string> a = from r in technologies
                                        where r.Contains("WLAN")
                                        select r;
                foreach (var i in a)
                {
                    if (i.Contains("WLANAC")) ac_exist = true;
                    else if (i.Contains("WLANAX")) ax_exist = true;
                    else if (i.Contains("WLANAY")) ay_exist = true;
                    else if (i.Contains("WLANA")) a_exist = true;
                    else if (i.Contains("WLANB")) b_exist = true;
                    else if (i.Contains("WLANG")) g_exist = true;
                    else if (i.Contains("WLANJ")) j_exist = true;
                    else if (i.Contains("WLANN")) n_exist = true;
                    else if (i.Contains("WLANP")) p_exist = true;
                }

                wlan_standards = new List<string>();
                if (a_exist) wlan_standards.Add("802.11A");
                if (b_exist) wlan_standards.Add("802.11B");
                if (g_exist) wlan_standards.Add("802.11G");
                if (j_exist) wlan_standards.Add("802.11J");
                if (n_exist) wlan_standards.Add("802.11N");
                if (p_exist) wlan_standards.Add("802.11P");
                if (ac_exist) wlan_standards.Add("802.11AC");
                if (ax_exist) wlan_standards.Add("802.11AX");
                if (ay_exist) wlan_standards.Add("802.11AY");
            }
        }
        private void BuildWlanBandwidthList()
        {
            string s;
            int start_idx;
            if (wlan_standard != null)
            {
                if (wlan_standard.Equals("802.11N"))
                {
                    s = "WLANN";
                    start_idx = 5;
                }
                else if (wlan_standard.Equals("802.11AC"))
                {
                    s = "WLANAC";
                    start_idx = 6;
                }
                else if (wlan_standard.Equals("802.11AX"))
                {
                    s = "WLANAX";
                    start_idx = 6;
                }
                else
                {
                    return;
                }

                IEnumerable<string> a = from r in technologies
                                        where r.Contains(s)
                                        select r.Substring(start_idx);

                bandwidths = a.ToList<string>();

                show_bandwidth = true;
            }
        }
        private void BuildLteFddBandwidthList()
        {
            if (format != null)
            {
                IEnumerable<string> a = from r in technologies
                                        where (r.Contains("LTE") && !r.Contains("LTETDD"))
                                        select r.Substring(3);
                bandwidths = a.ToList<string>();

                show_bandwidth = true;
            }
        }
        private void BuildLteTddBandwidthList()
        {
            if (format != null)
            {
                IEnumerable<string> a = from r in technologies
                                        where r.Contains("LTETDD")
                                        select r.Substring(6);
                bandwidths = a.ToList<string>();

                show_bandwidth = true;
            }
        }
        private void UpdateTechnology()
        {
            if (tech_select_method == TECH_SELECTION_METHOD.SELECT_MANUALLY)
            {
                if (format != null)
                {
                    if (!(format.Equals("LTE FDD") || format.Equals("LTE TDD") || format.Equals("WLAN")))
                    {
                        technology = format;
                    }
                    else if (format.Equals("LTE FDD") || format.Equals("LTE TDD"))
                    {
                        if (bandwidth != null)
                        {
                            string s = format.Equals("LTE FDD") ? "LTE" : "LTETDD";
                            technology = s + bandwidth;
                        }
                    }
                    else if (format.Equals("WLAN"))
                    {
                        if (wlan_standard != null && bandwidth != null)
                        {
                            technology = format + wlan_standard.Substring(6) + bandwidth;
                        }
                    }
                }
            }
        }

        public SelectTechnology2()
        {
            // Initialize a null PlugInMeas to fetch the technologies list
            if ((GetInstrument() != null) && (technology == null || technology == string.Empty))
            {
                PlugInMeas m = new PlugInMeas(null, null, null, null, null, null);
                bool ret = m.TestConditionSetup_LoadTestParameters(GetInstrument().config_file, GetInstrument().waveform_path);
                technologies = m.TestConditionSetup_GetLoadedTechnologies();
                IEnumerable<string> a = from r in technologies
                                        where !r.Contains("LTE") && !r.Contains("WLAN")
                                        select r;
                formats = a.ToList<string>();
                formats.Add("LTE FDD");
                formats.Add("LTE TDD");
                formats.Add("WLAN");
            }

            tech_select_method = TECH_SELECTION_METHOD.SELECT_FROM_LIST;
            is_dynamicEVM = false;

            duty_cycle = 1.0;
            lead_time = 1.0e-6;
            lag_time = 1.0e-6;
            trigger_delay = 10.0e-6;

            is_PolarPA = false;
            VrampVoltage = 1.2;
            PAEnableVoltage = 1.0;

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            string selected_waveform = null;
            try
            {
                technologies = GetInstrument().Measurement.TestConditionSetup_GetLoadedTechnologies();

                int triggerLineNumber = (int)GetInstrument().chassis_trigger_line;

                PlugInMeas meas = GetInstrument().Measurement;
                ret = meas.TestConditionSetup_SelectTechnology(
                    ref selected_waveform, technology, ref GetInstrument().iAwgVrampLoadedNumber,
                    ref GetInstrument().bVrampMaskLoaded, ref GetInstrument().VrampMask, 
                    is_dynamicEVM, is_seq, 
                    cw_waveform, lead_time,
                    lag_time,duty_cycle,
                    trigger_delay,PAEnableVoltage,
                    is_PolarPA, VrampVoltage, triggerLineNumber,
                    GetInstrument().use_secondary_source_analyzer, 
                    GetInstrument().secondary_source_analyzer_config);

                GetInstrument().selected_technology = technology;
                GetInstrument().selected_waveform = selected_waveform;
                GetInstrument().dDutycycle = duty_cycle;
                GetInstrument().dLeadTime = lead_time;
                GetInstrument().dLagTime = lag_time;
                GetInstrument().dRFTriggerDelay = trigger_delay;// wlan_evm_delay;

                PA_LOG(PA_LOG_TC, "TECH ", "[{2}] Select Technology {0}, Waveform {1}!", technology, GetInstrument().selected_waveform, ret ? "Pass" : "Fail");

                TestPlanRunningHelper.GetInstance().IsDPDActive = false;
                TestPlanRunningHelper.GetInstance().IsEtActive = false;

            }
            catch (Exception ex)
            {
                ret = false;
            }

            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            if (ret)
                UpgradeVerdict(Verdict.Pass);
            else
                UpgradeVerdict(Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }
    */

    [Display(Name: "Source & Analyzer", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("Source and Analyzer Setup. \nNOTE: Must do \"Select Technology\" first!")]
    [TcfVisible]
    public class Source_Analyzer_Setup : PA_TestStep
    {
        #region Settings
        [Display(Name: "Set Up Source", Order: -5)]
        public bool setup_source { get; set; }

        [Display(Name: "Set Up Analyzer", Order: -4)]
        public bool setup_analyzer { get; set; }

        [Display(Name: "Amplitude", Group: "Source", Order: 0.80)]
        [Unit("dBm")]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        [TcfVisible]
        public double src_amplitude { get; set; }

        [Display(Name: "Frequency", Group: "Source", Order: 0.81)]
        [Unit("MHz")]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        [TcfVisible]
        public double src_frequency { get; set; }

        [Display(Name: "Output Enabled", Group: "Source")]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public bool src_output_enabled { get; set; }

        [Display(Name: "Amplitude", Group: "Analyzer", Order: 0.90)]
        [Unit("dBm")]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        [TcfVisible]
        public double rcv_amplitude { get; set; }

        [Display(Name: "Frequency", Group: "Analyzer", Order: 0.91)]
        [Unit("MHz")]
        [Output]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        [TcfVisible]
        public double rcv_frequency { get; set; }

        [Display(Name: "Acquisition Mode", Group: "Analyzer", Order: 0.92)]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        public AnalyzerAcquisitionModeEnum rcv_acq_mode { get; set; }

        private bool _vsg_ext_trigger_enabled;
        [Display(Name: "Source Ext Trigger Enabled", Group: "Trigger", Order: 1)]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public bool vsg_ext_trigger_enabled
        {
            get { return _vsg_ext_trigger_enabled; }
            set
            {
                _vsg_ext_trigger_enabled = value;
                _vsg_sync_output_trigger_enabled = !_vsg_ext_trigger_enabled;
            }
        }

        [Display(Name: "Source Ext Trigger Source", Group: "Trigger", Order: 1.1)]
        [EnabledIf("vsg_ext_trigger_enabled", true, HideIfDisabled = true)]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public SourceTriggerEnum vsg_ext_trigger_source { get; set; }

        private bool _vsg_sync_output_trigger_enabled;
        [Display(Name: "Source Sync Output Trigger Enabled", Group: "Trigger", Order: 2)]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public bool sync_output_trigger_enabled
        {
            get { return _vsg_sync_output_trigger_enabled; }
            set
            {
                _vsg_sync_output_trigger_enabled = value;
                _vsg_ext_trigger_enabled = !_vsg_sync_output_trigger_enabled;
            }
        }

        [Display(Name: "Source Sync Output Trigger Destination", Group: "Trigger", Order: 2.1)]
        [EnabledIf("sync_output_trigger_enabled", true, HideIfDisabled = true)]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public SourceTriggerEnum vsg_sync_output_trigger_dest { get; set; }

        [Display(Name: "Source Sync Output Trigger Pulse Width", Group: "Trigger", Order: 3)]
        [Unit("s")]
        [EnabledIf("sync_output_trigger_enabled", true, HideIfDisabled = true)]
        [EnabledIf("setup_source", true, HideIfDisabled = true)]
        public double sync_output_trigger_pulse_width { get; set; }

        [Display(Name: "Analyzer Trigger Mode", Group: "Trigger", Order: 10)]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        [TcfVisible]
        public AnalyzerTriggerModeEnum triggerMode { get; set; }

        [Display(Name: "Analyzer Trigger Delay", Group: "Trigger", Order: 11)]
        [Unit("s")]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        public double triggerDelay { get; set; }

        [Display(Name: "Analyzer Trigger Timeout Mode", Group: "Trigger", Order: 12)]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        public AnalyzerTriggerTimeoutModeEnum timeoutMode { get; set; }

        [Display(Name: "Analyzer Trigger Timeout Value", Group: "Trigger", Order: 13)]
        [Unit("s")]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        [TcfVisible]
        public int timeout { get; set; }

        [Display(Name: "Analyzer External Trigger Source", Group: "Trigger", Order: 14)]
        [EnabledIf("triggerMode", AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal, HideIfDisabled = true)]
        [EnabledIf("setup_analyzer", true, HideIfDisabled = true)]
        public AnalyzerTriggerEnum extTriggerSource { get; set; }

        //[Display(Name: "Use System Calibration Data", Group: "Cable Loss", Order: 21)]
        //public bool use_cal_file { get; set; }

        //[Display(Name: "Nominal Input Loss", Group: "Cable Loss", Order: 22)]
        //[Unit("dB")]
        //[EnabledIf("use_cal_file", false, HideIfDisabled = true)]
        //public double nominal_input_loss { get; set; }

        //[Display(Name: "Nominal Output Loss", Group: "Cable Loss", Order: 23)]
        //[Unit("dB")]
        //[EnabledIf("use_cal_file", false, HideIfDisabled = true)]
        //public double nominal_output_loss { get; set; }

        #endregion

        public Source_Analyzer_Setup()
        {
            setup_source = true;
            setup_analyzer = true;
            src_frequency = 1700.0;
            src_amplitude = -30.0;
            src_output_enabled = true;
            rcv_frequency = 1700.0;
            rcv_amplitude = -15.0;
            rcv_acq_mode = AnalyzerAcquisitionModeEnum.AcquisitionModeFFT;
            vsg_ext_trigger_enabled = false;
            vsg_ext_trigger_source = SourceTriggerEnum.SourceTriggerPXITrigger2;
            vsg_sync_output_trigger_dest = SourceTriggerEnum.SourceTriggerPXITrigger2;
            sync_output_trigger_pulse_width = 10e-6;
            sync_output_trigger_enabled = true;
            triggerMode = AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal;
            triggerDelay = 0;
            timeoutMode = AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout;
            timeout = 10;
            extTriggerSource = AnalyzerTriggerEnum.TriggerPXITrigger2;
            //use_cal_file = true;
            //nominal_input_loss = 1.0;
            //nominal_output_loss = 1.0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            bool ret2 = true;
            try
            {
                IVSA vsa = GetInstrument().vsa;
                IVSG vsg = GetInstrument().vsg;
                IVXT vxt = GetInstrument().vxt;
                PlugInMeas meas = GetInstrument().Measurement;
                GetInstrument().receiver_trig_delay = triggerDelay;  

                ret = meas.TestConditionSetup_SourceReceiver(
                    src_frequency * 1e6, src_amplitude, src_output_enabled,
                    rcv_frequency * 1e6,
                    rcv_amplitude,
                    rcv_acq_mode,
                    AnalyzerFFTAcquisitionLengthEnum.FFTAcquisitionLength_512,
                    AnalyzerFFTWindowShapeEnum.FFTWindowShapeHann, setup_source, setup_analyzer, false,
                    GetInstrument().use_secondary_source_analyzer,
                    GetInstrument().secondary_source_analyzer_config);

                PA_LOG(PA_LOG_TC, "VSAG ", "[{0}] Source & Receiver Setup Done! Freq = {1,5:F0} MHz", ret ? "Pass" : "Fail", rcv_frequency);

                ret2 = meas.TestConditionSetup_Trigger(
                    vsg_ext_trigger_enabled, sync_output_trigger_enabled,
                    vsg_ext_trigger_source, vsg_sync_output_trigger_dest,
                    sync_output_trigger_pulse_width,
                    triggerMode, triggerDelay, timeoutMode, timeout, extTriggerSource, setup_source, setup_analyzer, true,
                    GetInstrument().use_secondary_source_analyzer,
                    GetInstrument().secondary_source_analyzer_config);

                PA_LOG(PA_LOG_TC, "TRIG ", "[{0}] Trigger Setup Done!", ret2 ? "Pass" : "Fail");

                ret = ret && ret2;
                GetInstrument().selected_frequency = rcv_frequency;
            }
            catch
            {
                ret = false;
            }

            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            if (ret)
                UpgradeVerdict(Verdict.Pass);
            else
                UpgradeVerdict(Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }





    [Display(Name: "VNA", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("VNA Setup")]
    [Browsable(false)]
    public class VNA_Setup : PA_TestStep
    {
        public enum VNA_SETUP_MODE
        {
            LOAD_FROM_FILE = 0,
            USER_DEFINED = 1
        }

        #region settings
        [Display(Name: "VNA Setup Mode", Group: "VNA Setup", Order: 1)]
        //[EnabledIf("set_vio", true, HideIfDisabled = true)]
        public VNA_SETUP_MODE vna_setup_mode { get; set; }

        [Display(Name: "VNA Setup File", Group: "VNA Setup", Order: 2)]
        [FilePath]
        //[EnabledIf("send_rffe_command", true, HideIfDisabled = true)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.LOAD_FROM_FILE, HideIfDisabled = true)]
        public string vna_setup_file { get; set; }

        [Display(Name: "Name", Description: "Trace name", Groups: new string[] { "Traces" }, Order: 3)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public string name { get; set; }

        [Display(Name: "Measurement", Description: "measurement", Groups: new string[] { "Traces" }, Order: 4)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public string measurement { get; set; }

        [Display(Name: "Format", Description: "Display Format", Groups: new string[] { "Traces" }, Order: 5)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public string Format { get; set; }

        [Display(Name: "Start Freq", Description: "Start Frequency", Groups: new string[] { "Segments" }, Order: 6)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public double start_freq { get; set; }

        [Display(Name: "Stop Freq", Description: "Stop Frequency", Groups: new string[] { "Segments" }, Order: 7)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public double stop_freq { get; set; }

        [Display(Name: "Number Points", Description: "Number points to be tested", Groups: new string[] { "Segments" }, Order: 8)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public double nummber_points { get; set; }

        [Display(Name: "IF bandwidth", Description: "IF bandwidth", Groups: new string[] { "Segments" }, Order: 9)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public double if_bandwidth { get; set; }

        [Display(Name: "Power", Description: "CW Power to be used", Groups: new string[] { "Segments" }, Order: 10)]
        [EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
        public double power { get; set; }

        #endregion



        public VNA_Setup()
        {

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            IVNA vna = GetInstrument().vna;

            if (vna_setup_mode == VNA_SETUP_MODE.LOAD_FROM_FILE)
            {
                ret = vna.readVnaSetupData(vna_setup_file);
                PA_LOG(PA_LOG_TC, "VNA  ", "[{0}] VNA readVnaSetupData!", ret ? "Pass" : "Fail");

                ret = vna.configSparms();
                PA_LOG(PA_LOG_TC, "VNA  ", "[{0}] VNA configSparms!", ret ? "Pass" : "Fail");
            }
            else
            {

            }

            RunChildSteps(); //If step has child steps.

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }


    [Display(Name: "Source & Analyzer Selection", Groups: new string[] { "PA", "Test Condition Setup" })]
    [Description("Choose whether use Primary or Secondary Source and Analyzer for Non-KMF Measurement")]
    [Browsable(true)]
    public class Secondary_Source_Analyzer_Configuration : PA_TestStep
    {

        #region settings
        [Display(Name: "Use Secondary Source/Analyzer for Measurement", Order: 1)]
        public bool use_secondary_source_analyzer { get; set; }
        #endregion

        public Secondary_Source_Analyzer_Configuration()
        {
            use_secondary_source_analyzer = false;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;

            PlugInMeas m = GetInstrument().Measurement;

            if (!use_secondary_source_analyzer)
            {
                if (GetInstrument().trx_config == PRIMARY_SOURCE_ANALYZER_CONFIG.VSA_AND_VSG)
                    GetInstrument().Measurement = new PlugInMeas(
                        GetInstrument().vsg,
                        GetInstrument().vsa,
                        GetInstrument().MeasCommon,
                        GetInstrument().vna,
                        GetInstrument().awg,
                        GetInstrument().pluginKmf);
                else if (GetInstrument().trx_config == PRIMARY_SOURCE_ANALYZER_CONFIG.VXT)
                    GetInstrument().Measurement = new PlugInMeas(
                        GetInstrument().vxt,
                        GetInstrument().MeasCommon,
                        GetInstrument().vna,
                        GetInstrument().awg,
                        GetInstrument().pluginKmf);
                else { }
            }
            else
            {
                if (GetInstrument().vxt2 == null)
                    GetInstrument().Measurement = new PlugInMeas(
                        GetInstrument().vsg,
                        GetInstrument().vsa,
                        GetInstrument().MeasCommon,
                        GetInstrument().vna,
                        GetInstrument().awg,
                        GetInstrument().pluginKmf,
                        GetInstrument().use_secondary_source_analyzer,
                        GetInstrument().secondary_source_analyzer_config,
                        GetInstrument().vsg2,
                        GetInstrument().vsa2,
                        GetInstrument().vxt2);
                else
                    GetInstrument().Measurement = new PlugInMeas(
                        GetInstrument().vxt,
                        GetInstrument().MeasCommon,
                        GetInstrument().vna,
                        GetInstrument().awg,
                        GetInstrument().pluginKmf,
                        GetInstrument().use_secondary_source_analyzer,
                        GetInstrument().secondary_source_analyzer_config,
                        GetInstrument().vsg2,
                        GetInstrument().vsa2,
                        GetInstrument().vxt2);

            }

            GetInstrument().Measurement.INIParam = m.INIParam;

            var tp = from i in GetInstrument().Measurement.INIParam.testParams where (i.technology.Equals(GetInstrument().selected_technology)) select i;
            if (tp != null && tp.FirstOrDefault() != null)
            {
                if (GetInstrument().vsa != null)
                {
                    GetInstrument().vsa.INIInfo = tp.FirstOrDefault();
                }
                if (GetInstrument().vsa2 != null)
                {
                    GetInstrument().vsa2.INIInfo = tp.FirstOrDefault();
                }
                if (GetInstrument().vxt != null)
                {
                    GetInstrument().vxt.INIInfo = tp.FirstOrDefault();
                }
            }

            PlugInBase b = GetInstrument().Measurement as PlugInBase;
            b.logData = GetInstrument().LogFunc;
            b.logEnabled = GetInstrument().generate_pa_log;

            // Reload Waveform & ConfigFile
            GetInstrument().Measurement.TestConditionSetup_LoadTestParameters(
                GetInstrument().config_file,
                GetInstrument().waveform_path,
                // If 2nd Source/Analyzer is VSA Only, no need to reload Arb
                (GetInstrument().secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY
                || GetInstrument().secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.NONE) ? false : true,
                GetInstrument().use_secondary_source_analyzer,
                GetInstrument().secondary_source_analyzer_config);

            GetInstrument().use_secondary_source_analyzer = use_secondary_source_analyzer;

            PA_LOG(PA_LOG_TC, "VSAG ", "[{3}] Use 2nd Source/Analyzer:{0}, 2nd Source/Analyzer Config:{1}, 2nd Analzyer Selected:{2}",
                use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config,
                GetInstrument().secondary_analyzer_selection, ret ? "Pass" : "Fail");

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    #endregion

    #region Measurements

    #region Test Frequency
    [Display(Name: "Set Test Frequency", Groups: new string[] { "PA", "Measurements" })]
    [Description("Set Test Frequency")]
    public class Set_TestFreqency : PA_TestStep
    {
        #region settings
        [Display(Name: "Frequency")]
        [Unit("MHz")]
        public double frequency { get; set; }

        #endregion
        public Set_TestFreqency()
        {
            frequency = 1700;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            Verdict v = Verdict.Pass;

            if (GetInstrument() == null || GetInstrument().vsg == null || GetInstrument().vsa == null)
            {
                Logging(LogLevel.ERROR, "PA_Instrument not initialized!");
                v = Verdict.Fail;
            }

            try
            {
                GetInstrument().vsg.Frequency = frequency * 1e6;
                GetInstrument().vsa.Frequency = frequency * 1e6;
                GetInstrument().vsg.Apply();
                GetInstrument().vsa.Apply();
                GetInstrument().selected_frequency = frequency;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Set Test Frequency Failed. {0}", ex.Message);
                v = Verdict.Fail;
            }

            PA_LOG(PA_LOG_TC, "FREQ ", "[{1}] Set Test Frequency to {0} MHz Done!", frequency, v == Verdict.Pass ? "Pass" : "Fail");

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(v);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }
    #endregion

    #region NON-KMF Measurement

    [Display(Name: "Power Servo", Groups: new string[] { "PA", "Measurements" })]
    [Description("Power Servo")]
    public class PA_PowerServo : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        public bool fft_servo { get; set; }

        #region POut
        //[Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        //public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        //[EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        //[EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region PIn
        [Display(Name: "Measure PIn", Group: "Test Items", Order: 103)]
        public bool meas_pin { get; set; }

        [Display(Name: "PIn High Limit", Group: "Test Items", Order: 104)]
        [Unit("dBm")]
        [EnabledIf("meas_pin", true, HideIfDisabled = true)]
        public double pin_high_limit { get; set; }

        [Display(Name: "PIn Low Limit", Group: "Test Items", Order: 105)]
        [Unit("dBm")]
        [EnabledIf("meas_pin", true, HideIfDisabled = true)]
        public double pin_low_limit { get; set; }
        #endregion

        #region Gain
        [Display(Name: "Measure Gain", Group: "Test Items", Order: 106)]
        public bool meas_gain { get; set; }

        [Display(Name: "PA Gain High Limit", Group: "Test Items", Order: 107)]
        [Unit("dB")]
        [EnabledIf("meas_gain", true, HideIfDisabled = true)]
        public double gain_high_limit { get; set; }

        [Display(Name: "PA Gain Low Limit", Group: "Test Items", Order: 108)]
        [Unit("dB")]
        [EnabledIf("meas_gain", true, HideIfDisabled = true)]
        public double gain_low_limit { get; set; }
        #endregion

        #endregion

        public PA_PowerServo()
        {
            frequency = 1700;
            target_pa_gain = 15;
            target_pa_output = -20;
            max_loop_count = 10;
            fft_servo = true;

            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            meas_pin = false;
            pin_high_limit = 30;
            pin_low_limit = -30;

            meas_gain = false;
            gain_high_limit = 50;
            gain_low_limit = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret;

            try
            {
                GetInstrument().selected_frequency = frequency;
                // Do Power Servo
                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(true, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps);
                MeasureOrNot(meas_pin, pin_high_limit, pin_low_limit, TestItem_Enum.PIN, tps);
                MeasureOrNot(meas_gain, gain_high_limit, gain_low_limit, TestItem_Enum.PA_GAIN, tps);

                GetInstrument().applied_target_pout = target_pa_output;

                Stopwatch sw = new Stopwatch();
                {
                    sw.Reset();
                    sw.Start();
                    ret = ps.ServoInputPower(frequency * 1e6,
                                            target_pa_output,
                                            target_pa_gain,
                                            fft_servo,
                                            max_loop_count,
                                            tps,
                                            GetInstrument().use_secondary_source_analyzer,
                                            GetInstrument().secondary_source_analyzer_config);
                    sw.Stop();
                }

                PublishResult2(true, GetInstrument().selected_technology, GetInstrument().selected_frequency, target_pa_output, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps, Results);
                PublishResult2(meas_pin, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, pin_high_limit, pin_low_limit, TestItem_Enum.PIN, tps, Results);
                PublishResult2(meas_gain, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, gain_high_limit, gain_low_limit, TestItem_Enum.PA_GAIN, tps, Results);

                PA_LOG(PA_LOG_TRS, "PS   ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, TargetPout:{3,9:F4}dBm, ActualPout:{4,9:F4}dBm, ActualGain:{5,9:F4}dB, Time:{6,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      target_pa_output,
                      (from r in tps where r.Key == TestItem_Enum.POUT select r.Value).FirstOrDefault().TestResult,
                      meas_gain ? (from r in tps where r.Key == TestItem_Enum.PA_GAIN select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

                if (GetInstrument().MeasConfigDict.ContainsKey(MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.POUT)))
                {
                    MeasureRealTimeDataConfig config = GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.POUT)] as MeasureRealTimeDataConfig;
                    if (config.EnableRealTime == true)
                    {
                        Dictionary<string, object> powerServoDic = new Dictionary<string, object>();
                        powerServoDic[MeasurementDataStructure.Value] = (from r in tps where r.Key == TestItem_Enum.POUT select r.Value).FirstOrDefault().TestResult;
                        powerServoDic[MeasurementDataStructure.IsSubMeas] = false;
                        powerServoDic[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                        powerServoDic[MeasurementDataStructure.MeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.POUT);
                        powerServoDic[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                        if (meas_pin)
                        {
                            powerServoDic[MeasurementDataStructure.PIn] = (from r in tps where r.Key == TestItem_Enum.PIN select r.Value).FirstOrDefault().TestResult;
                        }
                        else
                            powerServoDic[MeasurementDataStructure.PIn] = double.NaN;
                        if (meas_gain)
                        {
                            powerServoDic[MeasurementDataStructure.Gain] = (from r in tps where r.Key == TestItem_Enum.PA_GAIN select r.Value).FirstOrDefault()?.TestResult;
                        }
                        else
                            powerServoDic[MeasurementDataStructure.Gain] = double.NaN;

                        powerServoDic[MeasurementDataStructure.POut] = (from r in tps where r.Key == TestItem_Enum.POUT select r.Value).FirstOrDefault().TestResult;
                        powerServoDic[MeasurementDataStructure.TestTime] = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00;
                        powerServoDic[MeasurementDataStructure.DPDActive] = TestPlanRunningHelper.GetInstance().IsDPDActive;
                        powerServoDic[MeasurementDataStructure.ETActive] = TestPlanRunningHelper.GetInstance().IsEtActive;
                        powerServoDic[MeasurementDataStructure.Pass] = ret;


                        MeasurementDataCommunicationHelper.PublishRealTimeResults(powerServoDic);
                    }
                }
            }

            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "{0}", ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }


            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Combined Measurement", Groups: new string[] { "PA", "Measurements" })]
    [Description("Combined Measurement")]
    [Browsable(false)]
    public class PA_CombinedMeasurement : PA_TestStep
    {
        #region Settings
        [Unit("MHz")]
        public double frequency { get; set; }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        public bool fft_servo { get; set; }

        [Display(Name: "Duty Cycle", Group: "PAE", Order: 16)]
        [Description("The Duty Cycle for Power Added Efficiency measurement, in the range 0 ~ 1")]
        public double duty_cycle { get; set; }

        [Display(Name: "Edge", Group: "Harmonics", Order: 17)]
        [Description("Edge format to calculate Harmonics")]
        public bool edge { get; set; }

        #region Test Results

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region PIn
        [Display(Name: "Measure PIn", Group: "Test Items", Order: 103)]
        public bool meas_pin { get; set; }

        [Display(Name: "PIn High Limit", Group: "Test Items", Order: 104)]
        [Unit("dBm")]
        [EnabledIf("meas_pin", true, HideIfDisabled = true)]
        public double pin_high_limit { get; set; }

        [Display(Name: "PIn Low Limit", Group: "Test Items", Order: 105)]
        [Unit("dBm")]
        [EnabledIf("meas_pin", true, HideIfDisabled = true)]
        public double pin_low_limit { get; set; }
        #endregion

        #region Gain
        [Display(Name: "Measure Gain", Group: "Test Items", Order: 106)]
        public bool meas_gain { get; set; }

        [Display(Name: "PA Gain High Limit", Group: "Test Items", Order: 107)]
        [Unit("dB")]
        [EnabledIf("meas_gain", true, HideIfDisabled = true)]
        public double gain_high_limit { get; set; }

        [Display(Name: "PA Gain Low Limit", Group: "Test Items", Order: 108)]
        [Unit("dB")]
        [EnabledIf("meas_gain", true, HideIfDisabled = true)]
        public double gain_low_limit { get; set; }
        #endregion

        #region PAE
        [Display(Name: "PAE", Group: "Test Items", Order: 108.1)]
        public bool meas_pae { get; set; }

        [Display(Name: "PAE High Limit", Group: "Test Items", Order: 108.2)]
        [Unit("%")]
        [EnabledIf("meas_pae", true, HideIfDisabled = true)]
        public double pae_high_limit { get; set; }

        [Display(Name: "PAE Low Limit", Group: "Test Items", Order: 108.3)]
        [Unit("%")]
        [EnabledIf("meas_pae", true, HideIfDisabled = true)]
        public double pae_low_limit { get; set; }
        #endregion

        #region ICC
        [Display(Name: "ICC", Group: "Test Items", Order: 108.4)]
        public bool meas_icc { get; set; }

        [Display(Name: "ICC High Limit", Group: "Test Items", Order: 108.5)]
        [Unit("A")]
        [EnabledIf("meas_icc", true, HideIfDisabled = true)]
        public double icc_high_limit { get; set; }

        [Display(Name: "ICC Low Limit", Group: "Test Items", Order: 108.6)]
        [Unit("A")]
        [EnabledIf("meas_icc", true, HideIfDisabled = true)]
        public double icc_low_limit { get; set; }
        #endregion

        #region IBAT
        [Display(Name: "IBAT", Group: "Test Items", Order: 108.7)]
        public bool meas_ibat { get; set; }

        [Display(Name: "IBAT High Limit", Group: "Test Items", Order: 108.8)]
        [Unit("A")]
        [EnabledIf("meas_ibat", true, HideIfDisabled = true)]
        public double ibat_high_limit { get; set; }

        [Display(Name: "IBAT Low Limit", Group: "Test Items", Order: 108.9)]
        [Unit("A")]
        [EnabledIf("meas_ibat", true, HideIfDisabled = true)]
        public double ibat_low_limit { get; set; }
        #endregion

        #region ITOTAL
        [Display(Name: "ITOTAL", Group: "Test Items", Order: 108.91)]
        public bool meas_itotal { get; set; }

        [Display(Name: "ITOTAL High Limit", Group: "Test Items", Order: 108.92)]
        [Unit("A")]
        [EnabledIf("meas_itotal", true, HideIfDisabled = true)]
        public double itotal_high_limit { get; set; }

        [Display(Name: "ITOTAL Low Limit", Group: "Test Items", Order: 108.93)]
        [Unit("A")]
        [EnabledIf("meas_itotal", true, HideIfDisabled = true)]
        public double itotal_low_limit { get; set; }
        #endregion

        #region ACPR Lower 1
        [Display(Name: "Measure ACPR L1", Group: "Test Items", Order: 120)]
        public bool meas_acpr_l1 { get; set; }

        [Display(Name: "ACPR L1 High Limit", Group: "Test Items", Order: 121)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l1", true, HideIfDisabled = true)]
        public double acpr_l1_high_limit { get; set; }

        [Display(Name: "ACPR L1 Low Limit", Group: "Test Items", Order: 122)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l1", true, HideIfDisabled = true)]
        public double acpr_l1_low_limit { get; set; }
        #endregion

        #region ACPR Higher 1
        [Display(Name: "Measure ACPR H1", Group: "Test Items", Order: 124)]
        public bool meas_acpr_h1 { get; set; }

        [Display(Name: "ACPR H1 High Limit", Group: "Test Items", Order: 125)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h1", true, HideIfDisabled = true)]
        public double acpr_h1_high_limit { get; set; }

        [Display(Name: "ACPR H1 Low Limit", Group: "Test Items", Order: 126)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h1", true, HideIfDisabled = true)]
        public double acpr_h1_low_limit { get; set; }
        #endregion

        #region ACPR Lower 2
        [Display(Name: "Measure ACPR L2", Group: "Test Items", Order: 130)]
        public bool meas_acpr_l2 { get; set; }

        [Display(Name: "ACPR L2 High Limit", Group: "Test Items", Order: 131)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l2", true, HideIfDisabled = true)]
        public double acpr_l2_high_limit { get; set; }

        [Display(Name: "ACPR L2 Low Limit", Group: "Test Items", Order: 132)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l2", true, HideIfDisabled = true)]
        public double acpr_l2_low_limit { get; set; }
        #endregion

        #region ACPR Higher 2
        [Display(Name: "Measure ACPR H2", Group: "Test Items", Order: 134)]
        public bool meas_acpr_h2 { get; set; }

        [Display(Name: "ACPR H2 High Limit", Group: "Test Items", Order: 135)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h2", true, HideIfDisabled = true)]
        public double acpr_h2_high_limit { get; set; }

        [Display(Name: "ACPR H2 Low Limit", Group: "Test Items", Order: 136)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h2", true, HideIfDisabled = true)]
        public double acpr_h2_low_limit { get; set; }
        #endregion

        #region ACPR Lower 3
        [Display(Name: "Measure ACPR L3", Group: "Test Items", Order: 140)]
        public bool meas_acpr_l3 { get; set; }

        [Display(Name: "ACPR L3 High Limit", Group: "Test Items", Order: 141)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l3", true, HideIfDisabled = true)]
        public double acpr_l3_high_limit { get; set; }

        [Display(Name: "ACPR L3 Low Limit", Group: "Test Items", Order: 142)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l3", true, HideIfDisabled = true)]
        public double acpr_l3_low_limit { get; set; }
        #endregion

        #region ACPR Higher 3
        [Display(Name: "Measure ACPR H3", Group: "Test Items", Order: 144)]
        public bool meas_acpr_h3 { get; set; }

        [Display(Name: "ACPR H3 High Limit", Group: "Test Items", Order: 145)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h3", true, HideIfDisabled = true)]
        public double acpr_h3_high_limit { get; set; }

        [Display(Name: "ACPR H3 Low Limit", Group: "Test Items", Order: 146)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h3", true, HideIfDisabled = true)]
        public double acpr_h3_low_limit { get; set; }
        #endregion

        #region Harmonics - 2nd Order
        [Display(Name: "Measure 2nd Order Harmonics", Group: "Test Items", Order: 150)]
        public bool meas_harm2 { get; set; }

        [Display(Name: "2nd Order Harmonics High Limit", Group: "Test Items", Order: 151)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        public double harm2_high_limit { get; set; }

        [Display(Name: "2nd Order Harmonics Low Limit", Group: "Test Items", Order: 152)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        public double harm2_low_limit { get; set; }
        #endregion

        #region Harmonics - 3rd Order
        [Display(Name: "Measure 3rd Order Harmonics", Group: "Test Items", Order: 160)]
        public bool meas_harm3 { get; set; }

        [Display(Name: "3rd Order Harmonics High Limit", Group: "Test Items", Order: 161)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        public double harm3_high_limit { get; set; }

        [Display(Name: "3rd Order Harmonics Low Limit", Group: "Test Items", Order: 162)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        public double harm3_low_limit { get; set; }
        #endregion

        #region Harmonics - 4th Order
        [Display(Name: "Measure 4th Order Harmonics", Group: "Test Items", Order: 170)]
        public bool meas_harm4 { get; set; }

        [Display(Name: "4th Order Harmonics High Limit", Group: "Test Items", Order: 171)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        public double harm4_high_limit { get; set; }

        [Display(Name: "4th Order Harmonics Low Limit", Group: "Test Items", Order: 172)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        public double harm4_low_limit { get; set; }
        #endregion

        #region Harmonics - 5th Order
        [Display(Name: "Measure 5th Order Harmonics", Group: "Test Items", Order: 180)]
        public bool meas_harm5 { get; set; }

        [Display(Name: "5th Order Harmonics High Limit", Group: "Test Items", Order: 181)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        public double harm5_high_limit { get; set; }

        [Display(Name: "5th Order Harmonics Low Limit", Group: "Test Items", Order: 182)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        public double harm5_low_limit { get; set; }
        #endregion

        #endregion

        #endregion

        public PA_CombinedMeasurement()
        {
            frequency = 1700;
            target_pa_gain = 15;
            target_pa_output = -20;
            max_loop_count = 10;
            fft_servo = true;
            duty_cycle = 1;
            edge = false;

            meas_pout = false;
            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            meas_pin = false;
            pin_high_limit = 30;
            pin_low_limit = -30;

            meas_gain = false;
            gain_high_limit = 50;
            gain_low_limit = 0;

            meas_pae = false;
            pae_high_limit = 100;
            pae_low_limit = 0;

            meas_icc = false;
            icc_high_limit = 1;
            icc_low_limit = 0;

            meas_ibat = false;
            ibat_high_limit = 1;
            ibat_low_limit = 0;

            meas_itotal = false;
            itotal_high_limit = 2;
            itotal_low_limit = 0;

            meas_acpr_l1 = false;
            acpr_l1_high_limit = 0;
            acpr_l1_low_limit = -100;

            meas_acpr_h1 = false;
            acpr_h1_high_limit = 0;
            acpr_h1_low_limit = -100;

            meas_acpr_l2 = false;
            acpr_l2_high_limit = 0;
            acpr_l2_low_limit = -100;

            meas_acpr_h2 = false;
            acpr_h2_high_limit = 0;
            acpr_h2_low_limit = -100;

            meas_acpr_l3 = false;
            acpr_l3_high_limit = 0;
            acpr_l3_low_limit = -100;

            meas_acpr_h3 = false;
            acpr_h3_high_limit = 0;
            acpr_h3_low_limit = -100;

            meas_harm2 = false;
            harm2_high_limit = 0;
            harm2_low_limit = -100;

            meas_harm3 = false;
            harm3_high_limit = 0;
            harm3_low_limit = -100;

            meas_harm4 = false;
            harm4_high_limit = 0;
            harm4_low_limit = -100;

            meas_harm5 = false;
            harm5_high_limit = 0;
            harm5_low_limit = -100;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            Verdict verdict;
            bool ret;

            try
            {
                GetInstrument().selected_frequency = frequency;

                // Do Power Servo
                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(meas_pout, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps);
                MeasureOrNot(meas_pin, pin_high_limit, pin_low_limit, TestItem_Enum.PIN, tps);
                MeasureOrNot(meas_gain, gain_high_limit, gain_low_limit, TestItem_Enum.PA_GAIN, tps);
                MeasureOrNot(meas_pae, pae_high_limit, pae_low_limit, TestItem_Enum.PAE, tps);
                MeasureOrNot(meas_icc, icc_high_limit, icc_low_limit, TestItem_Enum.ICC, tps);
                MeasureOrNot(meas_ibat, ibat_high_limit, ibat_low_limit, TestItem_Enum.IBAT, tps);
                MeasureOrNot(meas_itotal, itotal_high_limit, itotal_low_limit, TestItem_Enum.ITOTAL, tps);

                MeasureOrNot(meas_acpr_l1, acpr_l1_high_limit, acpr_l1_low_limit, TestItem_Enum.ACPR_L1, tps);
                MeasureOrNot(meas_acpr_h1, acpr_h1_high_limit, acpr_h1_low_limit, TestItem_Enum.ACPR_H1, tps);
                MeasureOrNot(meas_acpr_l2, acpr_l2_high_limit, acpr_l2_low_limit, TestItem_Enum.ACPR_L2, tps);
                MeasureOrNot(meas_acpr_h2, acpr_h2_high_limit, acpr_h2_low_limit, TestItem_Enum.ACPR_H2, tps);
                MeasureOrNot(meas_acpr_l3, acpr_l3_high_limit, acpr_l3_low_limit, TestItem_Enum.ACPR_L3, tps);
                MeasureOrNot(meas_acpr_h3, acpr_h3_high_limit, acpr_h3_low_limit, TestItem_Enum.ACPR_H3, tps);

                MeasureOrNot(meas_harm2, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps);
                MeasureOrNot(meas_harm3, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps);
                MeasureOrNot(meas_harm4, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps);
                MeasureOrNot(meas_harm5, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps);

                Stopwatch sw = new Stopwatch();
                {
                    sw.Reset();
                    sw.Start();
                    ret = ps.CombinedMeasurement(frequency * 1e6,
                                            target_pa_output,
                                            target_pa_gain,
                                            fft_servo,
                                            max_loop_count,
                                            duty_cycle,
                                            edge,
                                            tps);
                    sw.Stop();
                    Logging(LogLevel.INFO, "ServoInputPower cost {0}", (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                }

                PublishResult2(meas_pout, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps, Results);
                PublishResult2(meas_pin, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, pin_high_limit, pin_low_limit, TestItem_Enum.PIN, tps, Results);
                PublishResult2(meas_gain, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, gain_high_limit, gain_low_limit, TestItem_Enum.PA_GAIN, tps, Results);
                PublishResult2(meas_pae, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, pae_high_limit, pae_low_limit, TestItem_Enum.PAE, tps, Results);
                PublishResult2(meas_icc, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, icc_high_limit, icc_low_limit, TestItem_Enum.ICC, tps, Results);
                PublishResult2(meas_ibat, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, ibat_high_limit, ibat_low_limit, TestItem_Enum.IBAT, tps, Results);
                PublishResult2(meas_itotal, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, itotal_high_limit, itotal_low_limit, TestItem_Enum.ITOTAL, tps, Results);

                PublishResult2(meas_acpr_l1, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l1_high_limit, acpr_l1_low_limit, TestItem_Enum.ACPR_L1, tps, Results);
                PublishResult2(meas_acpr_h1, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h1_high_limit, acpr_h1_low_limit, TestItem_Enum.ACPR_H1, tps, Results);
                PublishResult2(meas_acpr_l2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l2_high_limit, acpr_l2_low_limit, TestItem_Enum.ACPR_L2, tps, Results);
                PublishResult2(meas_acpr_h2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h2_high_limit, acpr_h2_low_limit, TestItem_Enum.ACPR_H2, tps, Results);
                PublishResult2(meas_acpr_l3, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l3_high_limit, acpr_l3_low_limit, TestItem_Enum.ACPR_L3, tps, Results);
                PublishResult2(meas_acpr_h3, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h3_high_limit, acpr_h3_low_limit, TestItem_Enum.ACPR_H3, tps, Results);

                PublishResult2(meas_harm2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps, Results);
                PublishResult2(meas_harm3, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps, Results);
                PublishResult2(meas_harm4, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps, Results);
                PublishResult2(meas_harm5, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps, Results);

                UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
            }

            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
            }


            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "ACPR", Groups: new string[] { "PA", "Measurements" })]
    [Description("ACPR Measurement")]
    public class PA_ACPRMeasurement : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        private bool _do_powerservo;
        [Display(Name: "Measure PowerServo", Group: "Power Servo", Order: 10)]
        public bool do_powerservo
        {
            get { return _do_powerservo; }
            set
            {
                _do_powerservo = value;
                meas_pout = _do_powerservo;
            }
        }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool fft_servo { get; set; }

        [Display(Name: "ACPR Type", Group: "ACPR", Order: 50)]
        [Browsable(false)]
        public ACPR_FORMAT_TYPE acpr_type { get; set; }

        [Display(Name: "GSM Numer Average", Group: "ACPR", Order: 52)]
        [EnabledIf("acpr_type", ACPR_FORMAT_TYPE.ACPR_GSM_FORMAT, HideIfDisabled = true)]
        public int gsm_num_avg { get; set; }

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region ACPR Lower 1
        [Display(Name: "Measure ACPR L1", Group: "Test Items", Order: 120)]
        public bool meas_acpr_l1 { get; set; }

        [Display(Name: "ACPR L1 High Limit", Group: "Test Items", Order: 121)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l1", true, HideIfDisabled = true)]
        public double acpr_l1_high_limit { get; set; }

        [Display(Name: "ACPR L1 Low Limit", Group: "Test Items", Order: 122)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l1", true, HideIfDisabled = true)]
        public double acpr_l1_low_limit { get; set; }
        #endregion

        #region ACPR Higher 1
        [Display(Name: "Measure ACPR H1", Group: "Test Items", Order: 124)]
        public bool meas_acpr_h1 { get; set; }

        [Display(Name: "ACPR H1 High Limit", Group: "Test Items", Order: 125)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h1", true, HideIfDisabled = true)]
        public double acpr_h1_high_limit { get; set; }

        [Display(Name: "ACPR H1 Low Limit", Group: "Test Items", Order: 126)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h1", true, HideIfDisabled = true)]
        public double acpr_h1_low_limit { get; set; }
        #endregion

        #region ACPR Lower 2
        [Display(Name: "Measure ACPR L2", Group: "Test Items", Order: 130)]
        public bool meas_acpr_l2 { get; set; }

        [Display(Name: "ACPR L2 High Limit", Group: "Test Items", Order: 131)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l2", true, HideIfDisabled = true)]
        public double acpr_l2_high_limit { get; set; }

        [Display(Name: "ACPR L2 Low Limit", Group: "Test Items", Order: 132)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l2", true, HideIfDisabled = true)]
        public double acpr_l2_low_limit { get; set; }
        #endregion

        #region ACPR Higher 2
        [Display(Name: "Measure ACPR H2", Group: "Test Items", Order: 134)]
        public bool meas_acpr_h2 { get; set; }

        [Display(Name: "ACPR H2 High Limit", Group: "Test Items", Order: 135)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h2", true, HideIfDisabled = true)]
        public double acpr_h2_high_limit { get; set; }

        [Display(Name: "ACPR H2 Low Limit", Group: "Test Items", Order: 136)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h2", true, HideIfDisabled = true)]
        public double acpr_h2_low_limit { get; set; }
        #endregion

        #region ACPR Lower 3
        [Display(Name: "Measure ACPR L3", Group: "Test Items", Order: 140)]
        public bool meas_acpr_l3 { get; set; }

        [Display(Name: "ACPR L3 High Limit", Group: "Test Items", Order: 141)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l3", true, HideIfDisabled = true)]
        public double acpr_l3_high_limit { get; set; }

        [Display(Name: "ACPR L3 Low Limit", Group: "Test Items", Order: 142)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_l3", true, HideIfDisabled = true)]
        public double acpr_l3_low_limit { get; set; }
        #endregion

        #region ACPR Higher 3
        [Display(Name: "Measure ACPR H3", Group: "Test Items", Order: 144)]
        public bool meas_acpr_h3 { get; set; }

        [Display(Name: "ACPR H3 High Limit", Group: "Test Items", Order: 145)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h3", true, HideIfDisabled = true)]
        public double acpr_h3_high_limit { get; set; }

        [Display(Name: "ACPR H3 Low Limit", Group: "Test Items", Order: 146)]
        [Unit("dB")]
        [EnabledIf("meas_acpr_h3", true, HideIfDisabled = true)]
        public double acpr_h3_low_limit { get; set; }
        #endregion


        #endregion

        public PA_ACPRMeasurement()
        {
            frequency = 1700.0;
            target_pa_gain = 15;
            target_pa_output = -20.0;
            max_loop_count = 10;
            fft_servo = true;
            acpr_type = ACPR_FORMAT_TYPE.ACPR_STANDARD_FORMAT;
            do_powerservo = true;
            gsm_num_avg = 1;

            meas_pout = true;
            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            meas_acpr_l1 = true;
            acpr_l1_high_limit = 0;
            acpr_l1_low_limit = -100;

            meas_acpr_h1 = true;
            acpr_h1_high_limit = 0;
            acpr_h1_low_limit = -100;

            meas_acpr_l2 = false;
            acpr_l2_high_limit = 0;
            acpr_l2_low_limit = -100;

            meas_acpr_h2 = false;
            acpr_h2_high_limit = 0;
            acpr_h2_low_limit = -100;

            meas_acpr_l3 = false;
            acpr_l3_high_limit = 0;
            acpr_l3_low_limit = -100;

            meas_acpr_h3 = false;
            acpr_h3_high_limit = 0;
            acpr_h3_low_limit = -100;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool pass = true;

            try
            {
                GetInstrument().selected_frequency = frequency;
                if (do_powerservo)
                {
                    GetInstrument().applied_target_pout = target_pa_output;
                }

                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;

                if (GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.GSM ||
                    GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.EDGE)
                {
                    acpr_type = ACPR_FORMAT_TYPE.ACPR_GSM_FORMAT;
                }
                else if (GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology).ToString().Contains("LTE"))
                {
                    acpr_type = ACPR_FORMAT_TYPE.ACPR_LTE_FORMAT;
                }
                else
                {
                    acpr_type = ACPR_FORMAT_TYPE.ACPR_STANDARD_FORMAT;
                }
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                if (do_powerservo)
                {
                    MeasureOrNot(meas_pout, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps);
                }

                MeasureOrNot(meas_acpr_l1, acpr_l1_high_limit, acpr_l1_low_limit, TestItem_Enum.ACPR_L1, tps);
                MeasureOrNot(meas_acpr_h1, acpr_h1_high_limit, acpr_h1_low_limit, TestItem_Enum.ACPR_H1, tps);
                MeasureOrNot(meas_acpr_l2, acpr_l2_high_limit, acpr_l2_low_limit, TestItem_Enum.ACPR_L2, tps);
                MeasureOrNot(meas_acpr_h2, acpr_h2_high_limit, acpr_h2_low_limit, TestItem_Enum.ACPR_H2, tps);
                MeasureOrNot(meas_acpr_l3, acpr_l3_high_limit, acpr_l3_low_limit, TestItem_Enum.ACPR_L3, tps);
                MeasureOrNot(meas_acpr_h3, acpr_h3_high_limit, acpr_h3_low_limit, TestItem_Enum.ACPR_H3, tps);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                pass = meas.MeasureAcpr(
                    acpr_type, do_powerservo, gsm_num_avg, frequency * 1e6,
                    target_pa_output, target_pa_gain, fft_servo, max_loop_count, tps,
                    GetInstrument().use_secondary_source_analyzer,
                    GetInstrument().secondary_source_analyzer_config);

                sw.Stop();

                if (do_powerservo)
                {
                    PublishResult2(meas_pout, GetInstrument().selected_technology, GetInstrument().selected_frequency, target_pa_output, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps, Results);
                }

                PublishResult2(meas_acpr_l1, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l1_high_limit, acpr_l1_low_limit, TestItem_Enum.ACPR_L1, tps, Results);
                PublishResult2(meas_acpr_h1, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h1_high_limit, acpr_h1_low_limit, TestItem_Enum.ACPR_H1, tps, Results);
                PublishResult2(meas_acpr_l2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l2_high_limit, acpr_l2_low_limit, TestItem_Enum.ACPR_L2, tps, Results);
                PublishResult2(meas_acpr_h2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h2_high_limit, acpr_h2_low_limit, TestItem_Enum.ACPR_H2, tps, Results);
                PublishResult2(meas_acpr_l3, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_l3_high_limit, acpr_l3_low_limit, TestItem_Enum.ACPR_L3, tps, Results);
                PublishResult2(meas_acpr_h3, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, acpr_h3_high_limit, acpr_h3_low_limit, TestItem_Enum.ACPR_H3, tps, Results);

                PA_LOG(PA_LOG_TRS, "ACPR ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, ACPR(L1:{3,9:F4}dB, H1:{4,9:F4}dB, L2:{5,9:F4}dB, H2:{6,9:F4}dB, L3:{7,9:F4}dB, H3:{8,9:F4}dB), Time:{9,7:F2}ms",
                      pass ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      meas_acpr_l1 ? (from r in tps where r.Key == TestItem_Enum.ACPR_L1 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_acpr_h1 ? (from r in tps where r.Key == TestItem_Enum.ACPR_H1 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_acpr_l2 ? (from r in tps where r.Key == TestItem_Enum.ACPR_L2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_acpr_h2 ? (from r in tps where r.Key == TestItem_Enum.ACPR_H2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_acpr_l3 ? (from r in tps where r.Key == TestItem_Enum.ACPR_L3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_acpr_h3 ? (from r in tps where r.Key == TestItem_Enum.ACPR_H3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    [Display(Name: "Harmonics", Groups: new string[] { "PA", "Measurements" })]
    [Description("Harmonics")]
    public class PA_HarmonicsMeasurement : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        private bool _do_powerservo;
        [Display(Name: "Measure PowerServo", Group: "Power Servo", Order: 10)]
        public bool do_powerservo
        {
            get { return _do_powerservo; }
            set
            {
                _do_powerservo = value;
                meas_pout = _do_powerservo;
            }
        }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool fft_servo { get; set; }

        [Display(Name: "Harmonics Result Format", Group: "Result Format", Order: 30)]
        public HARMONICS_RESULT_FORMAT result_format { get; set; }

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region Harmonics - 2nd Order
        [Display(Name: "Measure 2nd Order Harmonics", Group: "Test Items", Order: 150)]
        public bool meas_harm2 { get; set; }

        [Display(Name: "2nd Order Harmonics High Limit", Group: "Test Items", Order: 151)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm2_high_limit { get; set; }

        [Display(Name: "2nd Order Harmonics Low Limit", Group: "Test Items", Order: 152)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm2_low_limit { get; set; }

        [Display(Name: "2nd Order Harmonics High Limit", Group: "Test Items", Order: 151)]
        [Unit("dBm")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm2_high_limit2 { get; set; }

        [Display(Name: "2nd Order Harmonics Low Limit", Group: "Test Items", Order: 152)]
        [Unit("dBm")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm2_low_limit2 { get; set; }
        #endregion

        #region Harmonics - 3rd Order
        [Display(Name: "Measure 3rd Order Harmonics", Group: "Test Items", Order: 160)]
        public bool meas_harm3 { get; set; }

        [Display(Name: "3rd Order Harmonics High Limit", Group: "Test Items", Order: 161)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm3_high_limit { get; set; }

        [Display(Name: "3rd Order Harmonics Low Limit", Group: "Test Items", Order: 162)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm3_low_limit { get; set; }

        [Display(Name: "3rd Order Harmonics High Limit", Group: "Test Items", Order: 161)]
        [Unit("dBm")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm3_high_limit2 { get; set; }

        [Display(Name: "3rd Order Harmonics Low Limit", Group: "Test Items", Order: 162)]
        [Unit("dBm")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm3_low_limit2 { get; set; }
        #endregion

        #region Harmonics - 4th Order
        [Display(Name: "Measure 4th Order Harmonics", Group: "Test Items", Order: 170)]
        public bool meas_harm4 { get; set; }

        [Display(Name: "4th Order Harmonics High Limit", Group: "Test Items", Order: 171)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm4_high_limit { get; set; }

        [Display(Name: "4th Order Harmonics Low Limit", Group: "Test Items", Order: 172)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm4_low_limit { get; set; }

        [Display(Name: "4th Order Harmonics High Limit", Group: "Test Items", Order: 171)]
        [Unit("dBm")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm4_high_limit2 { get; set; }

        [Display(Name: "4th Order Harmonics Low Limit", Group: "Test Items", Order: 172)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm4_low_limit2 { get; set; }
        #endregion

        #region Harmonics - 5th Order
        [Display(Name: "Measure 5th Order Harmonics", Group: "Test Items", Order: 180)]
        public bool meas_harm5 { get; set; }

        [Display(Name: "5th Order Harmonics High Limit", Group: "Test Items", Order: 181)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm5_high_limit { get; set; }

        [Display(Name: "5th Order Harmonics Low Limit", Group: "Test Items", Order: 182)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, HideIfDisabled = true)]
        public double harm5_low_limit { get; set; }

        [Display(Name: "5th Order Harmonics High Limit", Group: "Test Items", Order: 181)]
        [Unit("dBm")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm5_high_limit2 { get; set; }

        [Display(Name: "5th Order Harmonics Low Limit", Group: "Test Items", Order: 182)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        [EnabledIf("result_format", HARMONICS_RESULT_FORMAT.DBM_PER_MHZ, HideIfDisabled = true)]
        public double harm5_low_limit2 { get; set; }
        #endregion

        #endregion

        public PA_HarmonicsMeasurement()
        {
            frequency = 1700;
            target_pa_gain = 15;
            target_pa_output = -20;
            max_loop_count = 10;
            fft_servo = true;
            do_powerservo = true;

            meas_pout = true;
            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            meas_harm2 = true;
            harm2_high_limit = 0;
            harm2_low_limit = -100;
            harm2_high_limit2 = 50;
            harm2_low_limit2 = -100;

            meas_harm3 = false;
            harm3_high_limit = 0;
            harm3_low_limit = -100;
            harm3_high_limit2 = 50;
            harm3_low_limit2 = -100;

            meas_harm4 = false;
            harm4_high_limit = 0;
            harm4_low_limit = -100;
            harm4_high_limit2 = 50;
            harm4_low_limit2 = -100;

            meas_harm5 = false;
            harm5_high_limit = 0;
            harm5_low_limit = -100;
            harm5_high_limit2 = 50;
            harm5_low_limit2 = -100;

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            try
            {
                GetInstrument().selected_frequency = frequency;
                if (do_powerservo)
                {
                    GetInstrument().applied_target_pout = target_pa_output;
                }

                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(meas_pout, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps);

                if (result_format == HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL)
                {
                    MeasureOrNot(meas_harm2, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps);
                    MeasureOrNot(meas_harm3, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps);
                    MeasureOrNot(meas_harm4, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps);
                    MeasureOrNot(meas_harm5, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps);
                }
                else
                {
                    MeasureOrNot(meas_harm2, harm2_high_limit2, harm2_low_limit2, TestItem_Enum.HARMONICS_2, tps);
                    MeasureOrNot(meas_harm3, harm3_high_limit2, harm3_low_limit2, TestItem_Enum.HARMONICS_3, tps);
                    MeasureOrNot(meas_harm4, harm4_high_limit2, harm4_low_limit2, TestItem_Enum.HARMONICS_4, tps);
                    MeasureOrNot(meas_harm5, harm5_high_limit2, harm5_low_limit2, TestItem_Enum.HARMONICS_5, tps);
                }

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                ret = meas.MeasureHarmonics(
                    do_powerservo,
                    (meas.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.EDGE),
                    frequency * 1e6, target_pa_output, target_pa_gain,
                    fft_servo, max_loop_count, tps, result_format,
                    GetInstrument().use_secondary_source_analyzer,
                    GetInstrument().secondary_source_analyzer_config);
                sw.Stop();

                if (do_powerservo)
                {
                    PublishResult2(meas_pout, GetInstrument().selected_technology, GetInstrument().selected_frequency, target_pa_output, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps, Results);
                }

                if (result_format == HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL)
                {
                    PublishResult2(meas_harm2, GetInstrument().selected_technology, GetInstrument().selected_frequency * 2, GetInstrument().applied_target_pout, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps, Results);
                    PublishResult2(meas_harm3, GetInstrument().selected_technology, GetInstrument().selected_frequency * 3, GetInstrument().applied_target_pout, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps, Results);
                    PublishResult2(meas_harm4, GetInstrument().selected_technology, GetInstrument().selected_frequency * 4, GetInstrument().applied_target_pout, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps, Results);
                    PublishResult2(meas_harm5, GetInstrument().selected_technology, GetInstrument().selected_frequency * 5, GetInstrument().applied_target_pout, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps, Results);

                    PA_LOG(PA_LOG_TRS, "HARM ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, HARM(2nd:{3,9:F4}dB, 3rd:{4,9:F4}dB, 4th:{5,9:F4}dB, 5th:{6,9:F4}dB), Time:{7,7:F2}ms",
                          ret ? "Pass" : "Fail",
                          TechnologyName(GetInstrument().selected_technology),
                          GetInstrument().selected_frequency,
                          meas_harm2 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm3 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm4 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_4 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm5 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_5 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                }
                else
                {
                    PublishResult2(meas_harm2, GetInstrument().selected_technology, GetInstrument().selected_frequency * 2, GetInstrument().applied_target_pout, harm2_high_limit2, harm2_low_limit2, TestItem_Enum.HARMONICS_2, tps, Results);
                    PublishResult2(meas_harm3, GetInstrument().selected_technology, GetInstrument().selected_frequency * 3, GetInstrument().applied_target_pout, harm3_high_limit2, harm3_low_limit2, TestItem_Enum.HARMONICS_3, tps, Results);
                    PublishResult2(meas_harm4, GetInstrument().selected_technology, GetInstrument().selected_frequency * 4, GetInstrument().applied_target_pout, harm4_high_limit2, harm4_low_limit2, TestItem_Enum.HARMONICS_4, tps, Results);
                    PublishResult2(meas_harm5, GetInstrument().selected_technology, GetInstrument().selected_frequency * 5, GetInstrument().applied_target_pout, harm5_high_limit2, harm5_low_limit2, TestItem_Enum.HARMONICS_5, tps, Results);

                    PA_LOG(PA_LOG_TRS, "HARM ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, HARM(2nd:{3,9:F4}dBm/MHz, 3rd:{4,9:F4}dBm/MHz, 4th:{5,9:F4}dBm/MHz, 5th:{6,9:F4}dBm/MHz), Time:{7,7:F2}ms",
                          ret ? "Pass" : "Fail",
                          TechnologyName(GetInstrument().selected_technology),
                          GetInstrument().selected_frequency,
                          meas_harm2 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm3 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm4 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_4 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_harm5 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_5 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                }


                PA_LOG(PA_LOG_TRS, "HARM ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, HARM(2nd:{3,9:F4}dBm/MHz, 3rd:{4,9:F4}dBm/MHz, 4th:{5,9:F4}dBm/MHz, 5th:{6,9:F4}dBm/MHz), Time:{7,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      meas_harm2 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_harm3 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_harm4 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_4 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_harm5 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_5 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);



                if (ret)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    [Display(Name: "Harmonics(CXA-m)", Groups: new string[] { "PA", "Measurements" })]
    [Description("Harmonics on CXA-m")]
    public class PA_HarmonicsMeasurement_CXAm : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        [Display(Name: "Ref Level", Order: 2)]
        [Unit("dBm")]
        public double refLevel { get; set; }

        private AnalyzerTriggerModeEnum _trig_mode;
        [Display(Name: "Trigger Mode", Order: 3)]
        public AnalyzerTriggerModeEnum trig_mode
        {
            get { return _trig_mode; }
            set
            {
                _trig_mode = value;
                if (_trig_mode != AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal &&
                    _trig_mode != AnalyzerTriggerModeEnum.AcquisitionTriggerModeImmediate)
                {
                    Log.Warning("Trigger Mode {0} is not supported, use Immediate instead!", _trig_mode);
                    _trig_mode = AnalyzerTriggerModeEnum.AcquisitionTriggerModeImmediate;
                }
            }
        }

        private AnalyzerTriggerEnum _trig_line;
        [Display(Name: "Trigger Line", Order: 4)]
        [EnabledIf("trig_mode", AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal, HideIfDisabled = true)]
        public AnalyzerTriggerEnum trig_line
        {
            get { return _trig_line; }
            set
            {
                _trig_line = value;
                if (_trig_line == AnalyzerTriggerEnum.TriggerFrontPanelTrigger2)
                {
                    Log.Warning("Front Panel Trigger 2 is not supported, use Front Panel Trigger 1 instead!");
                    _trig_line = AnalyzerTriggerEnum.TriggerFrontPanelTrigger1;
                }
            }
        }

        private int _num_harms;
        [Display(Name: "Number of Harmonics to Measure", Order: 10)]
        public int num_harms
        {
            get { return _num_harms; }
            set
            {
                _num_harms = value;
                if (_num_harms < 2) _num_harms = 2;
                if (_num_harms > 5) _num_harms = 5;

                if (_num_harms >= 2) meas_harm2 = true; else meas_harm2 = false;
                if (_num_harms >= 3) meas_harm3 = true; else meas_harm3 = false;
                if (_num_harms >= 4) meas_harm4 = true; else meas_harm4 = false;
                if (_num_harms >= 5) meas_harm5 = true; else meas_harm5 = false;
            }
        }

        #region Harmonics - 2nd Order
        [Display(Name: "Measure 2nd Order Harmonics", Group: "Test Items", Order: 150)]
        [Browsable(false)]
        public bool meas_harm2 { get; set; }

        [Display(Name: "2nd Order Harmonics High Limit", Group: "Test Items", Order: 151)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        public double harm2_high_limit { get; set; }

        [Display(Name: "2nd Order Harmonics Low Limit", Group: "Test Items", Order: 152)]
        [Unit("dB")]
        [EnabledIf("meas_harm2", true, HideIfDisabled = true)]
        public double harm2_low_limit { get; set; }
        #endregion

        #region Harmonics - 3rd Order
        [Display(Name: "Measure 3rd Order Harmonics", Group: "Test Items", Order: 160)]
        [Browsable(false)]
        public bool meas_harm3 { get; set; }

        [Display(Name: "3rd Order Harmonics High Limit", Group: "Test Items", Order: 161)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        public double harm3_high_limit { get; set; }

        [Display(Name: "3rd Order Harmonics Low Limit", Group: "Test Items", Order: 162)]
        [Unit("dB")]
        [EnabledIf("meas_harm3", true, HideIfDisabled = true)]
        public double harm3_low_limit { get; set; }
        #endregion

        #region Harmonics - 4th Order
        [Display(Name: "Measure 4th Order Harmonics", Group: "Test Items", Order: 170)]
        [Browsable(false)]
        public bool meas_harm4 { get; set; }

        [Display(Name: "4th Order Harmonics High Limit", Group: "Test Items", Order: 171)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        public double harm4_high_limit { get; set; }

        [Display(Name: "4th Order Harmonics Low Limit", Group: "Test Items", Order: 172)]
        [Unit("dB")]
        [EnabledIf("meas_harm4", true, HideIfDisabled = true)]
        public double harm4_low_limit { get; set; }
        #endregion

        #region Harmonics - 5th Order
        [Display(Name: "Measure 5th Order Harmonics", Group: "Test Items", Order: 180)]
        [Browsable(false)]
        public bool meas_harm5 { get; set; }

        [Display(Name: "5th Order Harmonics High Limit", Group: "Test Items", Order: 181)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        public double harm5_high_limit { get; set; }

        [Display(Name: "5th Order Harmonics Low Limit", Group: "Test Items", Order: 182)]
        [Unit("dB")]
        [EnabledIf("meas_harm5", true, HideIfDisabled = true)]
        public double harm5_low_limit { get; set; }
        #endregion

        #endregion

        public PA_HarmonicsMeasurement_CXAm()
        {
            frequency = 1700;
            refLevel = 0;
            trig_mode = AnalyzerTriggerModeEnum.AcquisitionTriggerModeImmediate;
            trig_line = AnalyzerTriggerEnum.TriggerPXITrigger2;
            num_harms = 2;

            meas_harm2 = true;
            harm2_high_limit = 0;
            harm2_low_limit = -100;

            meas_harm3 = false;
            harm3_high_limit = 0;
            harm3_low_limit = -100;

            meas_harm4 = false;
            harm4_high_limit = 0;
            harm4_low_limit = -100;

            meas_harm5 = false;
            harm5_high_limit = 0;
            harm5_low_limit = -100;

        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret = true;
            try
            {
                GetInstrument().selected_frequency = frequency;

                // ToDo: Add test case code here
                IVSA cxa = GetInstrument().cxa;
                PlugInMeas meas = GetInstrument().Measurement;
                cxa.INIInfo = GetInstrument().vsa.INIInfo;
                meas.cxa_m = cxa;

                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                bool[] harmsToMeas = new bool[] { true, meas_harm2, meas_harm3, meas_harm4, meas_harm5 };

                MeasureOrNot(meas_harm2, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps);
                MeasureOrNot(meas_harm3, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps);
                MeasureOrNot(meas_harm4, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps);
                MeasureOrNot(meas_harm5, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                ret = meas.MeasureHarmonics_CXAm(frequency * 1e6, refLevel, trig_mode, trig_line, harmsToMeas, tps);
                sw.Stop();

                PublishResult2(meas_harm2, GetInstrument().selected_technology, GetInstrument().selected_frequency * 2, GetInstrument().applied_target_pout, harm2_high_limit, harm2_low_limit, TestItem_Enum.HARMONICS_2, tps, Results);
                PublishResult2(meas_harm3, GetInstrument().selected_technology, GetInstrument().selected_frequency * 3, GetInstrument().applied_target_pout, harm3_high_limit, harm3_low_limit, TestItem_Enum.HARMONICS_3, tps, Results);
                PublishResult2(meas_harm4, GetInstrument().selected_technology, GetInstrument().selected_frequency * 4, GetInstrument().applied_target_pout, harm4_high_limit, harm4_low_limit, TestItem_Enum.HARMONICS_4, tps, Results);
                PublishResult2(meas_harm5, GetInstrument().selected_technology, GetInstrument().selected_frequency * 5, GetInstrument().applied_target_pout, harm5_high_limit, harm5_low_limit, TestItem_Enum.HARMONICS_5, tps, Results);

                PA_LOG(PA_LOG_TRS, "HARM ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, HARM(2nd:{3,9:F4}dB, 3rd:{4,9:F4}dB, 4th:{5,9:F4}dB, 5th:{6,9:F4}dB), Time:{7,7:F2}ms",
                        ret ? "Pass" : "Fail",
                        TechnologyName(GetInstrument().selected_technology),
                        GetInstrument().selected_frequency,
                        meas_harm2 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_2 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                        meas_harm3 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_3 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                        meas_harm4 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_4 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                        meas_harm5 ? (from r in tps where r.Key == TestItem_Enum.HARMONICS_5 select r.Value).FirstOrDefault().TestResult : Double.NaN,
                        (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (ret)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }


    [Display(Name: "PAE", Groups: new string[] { "PA", "Measurements" })]
    [Description("PAE")]
    public class PA_PAEMeasurement : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        private bool _do_powerservo;
        [Display(Name: "Measure PowerServo", Group: "Power Servo", Order: 10)]
        public bool do_powerservo
        {
            get { return _do_powerservo; }
            set
            {
                _do_powerservo = value;
                meas_pout = _do_powerservo;
            }
        }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool fft_servo { get; set; }


        [Display(Name: "Duty Cycle", Group: "PAE", Order: 55)]
        [Description("The Duty Cycle for Power Added Efficiency measurement")]
        public double duty_cycle { get; set; }

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region PAE
        [Display(Name: "PAE", Group: "Test Items", Order: 108.1)]
        public bool meas_pae { get; set; }

        [Display(Name: "PAE High Limit", Group: "Test Items", Order: 108.2)]
        [Unit("%")]
        [EnabledIf("meas_pae", true, HideIfDisabled = true)]
        public double pae_high_limit { get; set; }

        [Display(Name: "PAE Low Limit", Group: "Test Items", Order: 108.3)]
        [Unit("%")]
        [EnabledIf("meas_pae", true, HideIfDisabled = true)]
        public double pae_low_limit { get; set; }
        #endregion

        #endregion

        public PA_PAEMeasurement()
        {
            frequency = 1700;
            target_pa_gain = 15;
            target_pa_output = -20;
            max_loop_count = 10;
            fft_servo = true;
            duty_cycle = 1;
            do_powerservo = true;

            meas_pout = true;
            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            meas_pae = true;
            pae_high_limit = 100;
            pae_low_limit = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {
            bool ret = true;
            try
            {
                GetInstrument().selected_frequency = frequency;
                if (do_powerservo)
                {
                    GetInstrument().applied_target_pout = target_pa_output;
                }

                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(meas_pout, target_pa_output + pout_high_limit, target_pa_output + pout_low_limit, TestItem_Enum.POUT, tps);
                MeasureOrNot(meas_pae, pae_high_limit, pae_low_limit, TestItem_Enum.PAE, tps);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                ret = meas.MeasurePAE(do_powerservo, duty_cycle, frequency * 1e6, target_pa_output, target_pa_gain,
                    fft_servo, max_loop_count, tps,
                    GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                sw.Stop();

                if (do_powerservo)
                {
                    PublishResult2(meas_pout, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, GetInstrument().applied_target_pout + pout_high_limit, GetInstrument().applied_target_pout + pout_low_limit, TestItem_Enum.POUT, tps, Results);
                }
                PublishResult2(meas_pae, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, pae_high_limit, pae_low_limit, TestItem_Enum.PAE, tps, Results);

                PA_LOG(PA_LOG_TRS, "PAE  ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, PAE:{3,9:F4}%, Time:{4,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      meas_pae ? (from r in tps where r.Key == TestItem_Enum.PAE select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (ret)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);

                if (GetInstrument().MeasConfigDict.ContainsKey(MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.PAE)))
                {
                    MeasureRealTimeDataConfig config = GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.PAE)] as MeasureRealTimeDataConfig;
                    if (config.EnableRealTime == true)
                    {
                        Dictionary<string, object> paeDic = new Dictionary<string, object>();
                        paeDic[MeasurementDataStructure.Value] = (from r in tps where r.Key == TestItem_Enum.PAE select r.Value).FirstOrDefault().TestResult;
                        paeDic[MeasurementDataStructure.IsSubMeas] = false;
                        paeDic[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                        paeDic[MeasurementDataStructure.MeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.PAE);
                        paeDic[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                        paeDic[MeasurementDataStructure.PIn] = double.NaN;
                        paeDic[MeasurementDataStructure.Gain] = double.NaN;
                        paeDic[MeasurementDataStructure.DPDActive] = TestPlanRunningHelper.GetInstance().IsDPDActive;
                        paeDic[MeasurementDataStructure.ETActive] = TestPlanRunningHelper.GetInstance().IsEtActive;
                        if (meas_pout)
                        {
                            paeDic[MeasurementDataStructure.POut] = (from r in tps where r.Key == TestItem_Enum.POUT select r.Value).FirstOrDefault().TestResult;
                        }
                        else
                            paeDic[MeasurementDataStructure.POut] = double.NaN;

                        paeDic[MeasurementDataStructure.TestTime] = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00;
                        paeDic[MeasurementDataStructure.Pass] = ret;

                        MeasurementDataCommunicationHelper.PublishRealTimeResults(paeDic);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }

    [Display(Name: "Gain Compression", Groups: new string[] { "PA", "Measurements" })]
    [Description("Gain Compression")]
    public class PA_GainCompression : PA_TestStep
    {
        #region Settings
        [Display(Name: "DUT Input Power", Order: 1)]
        [Description("The maximum DUT Input power of the Power Ramp Waveform")]
        [Unit("dBm")]
        public double dut_input_power { get; set; }

        #region 1dB Compression point
        [Display(Name: "Measure 1dB Compression Point", Group: "Test Items", Order: 100)]
        public bool meas_1db_comp { get; set; }

        [Display(Name: "1dB Compression Point High Limit", Group: "Test Items", Order: 101)]
        [Unit("dBm")]
        [EnabledIf("meas_1db_comp", true, HideIfDisabled = true)]
        public double _1db_comp_high_limit { get; set; }

        [Display(Name: "1dB Compression Point Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dBm")]
        [EnabledIf("meas_1db_comp", true, HideIfDisabled = true)]
        public double _1db_comp_low_limit { get; set; }
        #endregion

        #region 2dB Compression point
        [Display(Name: "Measure 2dB Compression Point", Group: "Test Items", Order: 110)]
        public bool meas_2db_comp { get; set; }

        [Display(Name: "2dB Compression Point High Limit", Group: "Test Items", Order: 111)]
        [Unit("dBm")]
        [EnabledIf("meas_2db_comp", true, HideIfDisabled = true)]
        public double _2db_comp_high_limit { get; set; }

        [Display(Name: "2dB Compression Point Low Limit", Group: "Test Items", Order: 112)]
        [Unit("dBm")]
        [EnabledIf("meas_2db_comp", true, HideIfDisabled = true)]
        public double _2db_comp_low_limit { get; set; }
        #endregion

        #region 3dB Compression point
        [Display(Name: "Measure 3dB Compression Point", Group: "Test Items", Order: 120)]
        public bool meas_3db_comp { get; set; }

        [Display(Name: "3dB Compression Point High Limit", Group: "Test Items", Order: 121)]
        [Unit("dBm")]
        [EnabledIf("meas_3db_comp", true, HideIfDisabled = true)]
        public double _3db_comp_high_limit { get; set; }

        [Display(Name: "3dB Compression Point Low Limit", Group: "Test Items", Order: 122)]
        [Unit("dBm")]
        [EnabledIf("meas_3db_comp", true, HideIfDisabled = true)]
        public double _3db_comp_low_limit { get; set; }
        #endregion

        #endregion

        public PA_GainCompression()
        {
            dut_input_power = 5;

            meas_1db_comp = true;
            _1db_comp_low_limit = -15;
            _1db_comp_high_limit = 15;

            meas_2db_comp = false;
            _2db_comp_low_limit = -15;
            _2db_comp_high_limit = 20;

            meas_3db_comp = false;
            _3db_comp_low_limit = -15;
            _3db_comp_high_limit = 30;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;

            try
            {
                // Do Gain Compression measurement
                PlugInMeas pm = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(meas_1db_comp, _1db_comp_high_limit, _1db_comp_low_limit, TestItem_Enum.GAIN_COMP_1DB, tps);
                MeasureOrNot(meas_2db_comp, _2db_comp_high_limit, _2db_comp_low_limit, TestItem_Enum.GAIN_COMP_2DB, tps);
                MeasureOrNot(meas_3db_comp, _3db_comp_high_limit, _3db_comp_low_limit, TestItem_Enum.GAIN_COMP_3DB, tps);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                ret = pm.MeasureGainComp(dut_input_power, tps,
                    GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                sw.Stop();

                PublishResult1(meas_1db_comp, GetInstrument().selected_technology, GetInstrument().selected_frequency, _1db_comp_high_limit, _1db_comp_low_limit, TestItem_Enum.GAIN_COMP_1DB, tps, Results);
                PublishResult1(meas_2db_comp, GetInstrument().selected_technology, GetInstrument().selected_frequency, _2db_comp_high_limit, _2db_comp_low_limit, TestItem_Enum.GAIN_COMP_2DB, tps, Results);
                PublishResult1(meas_3db_comp, GetInstrument().selected_technology, GetInstrument().selected_frequency, _3db_comp_high_limit, _3db_comp_low_limit, TestItem_Enum.GAIN_COMP_3DB, tps, Results);

                PA_LOG(PA_LOG_TRS, "GCMP ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, GainCompression(1dB:{3,9:F4}dBm, 2dB:{4,9:F4}dBm, 3dB:{5,9:F4}dBm), Time:{6,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      meas_1db_comp ? (from r in tps where r.Key == TestItem_Enum.GAIN_COMP_1DB select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_2db_comp ? (from r in tps where r.Key == TestItem_Enum.GAIN_COMP_2DB select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      meas_3db_comp ? (from r in tps where r.Key == TestItem_Enum.GAIN_COMP_3DB select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
            }

            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
            }

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "DUT Output Power", Groups: new string[] { "PA", "Measurements" })]
    [Description("Dut Output Power")]
    public class PA_DutOutputPower : PA_TestStep
    {
        #region Settings
        [Display(Name: "Frequency", Order: 1)]
        [Unit("MHz")]
        public double frequency { get; set; }

        [Display(Name: "DUT Input Power", Order: 10)]
        [Unit("dBm")]
        public double dut_input_power { get; set; }

        [Display(Name: "PA Gain", Order: 11)]
        [Unit("dB")]
        public double target_pa_gain { get; set; }

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dBm")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dBm")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion


        #endregion

        public PA_DutOutputPower()
        {
            frequency = 1700;
            target_pa_gain = 15;
            dut_input_power = -20;

            meas_pout = true;
            pout_high_limit = -19.9;
            pout_low_limit = -20.1;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret;
            try
            {
                GetInstrument().selected_frequency = frequency;

                // Do Gain Compression measurement
                PlugInMeas pm = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                MeasureOrNot(meas_pout, pout_high_limit, pout_low_limit, TestItem_Enum.POUT, tps);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                ret = pm.MeasureDutOutputPower(frequency * 1e6, dut_input_power, target_pa_gain, tps,
                    GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                sw.Stop();

                PublishResult1(meas_pout, GetInstrument().selected_technology, GetInstrument().selected_frequency, pout_high_limit, pout_low_limit, TestItem_Enum.POUT, tps, Results);

                PA_LOG(PA_LOG_TRS, "DOP  ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Dut Output Power:{3,9:F4}dBm, Time:{4,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      meas_pout ? (from r in tps where r.Key == TestItem_Enum.POUT select r.Value).FirstOrDefault().TestResult : Double.NaN,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (ret)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }

            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
            }

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Measure Current", Groups: new string[] { "PA", "Measurements" })]
    [Description("Measure Current")]
    public class PA_MeasureCurrent : PA_TestStep
    {
        #region Settings

        private DCSMUInfo.SMU_CHANTYPE _meas_type;

        public enum SENSE_RANGE
        {
            RANGE_3A = 0,
            RANGE_1mA,
            RANGE_100uA
        }

        [Display(Name: "Current Range", Order: 1)]
        public SENSE_RANGE sense_range { get; set; }

        #region ICC
        [Display(Name: "Measure ICC", Group: "Test Items", Order: 100)]
        public bool meas_icc { get; set; }

        [Display(Name: "ICC High Limit", Group: "Test Items", Order: 101)]
        [Unit("A")]
        [EnabledIf("meas_icc", true, HideIfDisabled = true)]
        public double icc_high_limit { get; set; }

        [Display(Name: "ICC Low Limit", Group: "Test Items", Order: 102)]
        [Unit("A")]
        [EnabledIf("meas_icc", true, HideIfDisabled = true)]
        public double icc_low_limit { get; set; }
        #endregion

        #region IBAT
        [Display(Name: "Measure IBAT", Group: "Test Items", Order: 200)]
        public bool meas_ibat { get; set; }

        [Display(Name: "IBAT High Limit", Group: "Test Items", Order: 201)]
        [Unit("A")]
        [EnabledIf("meas_ibat", true, HideIfDisabled = true)]
        public double ibat_high_limit { get; set; }

        [Display(Name: "IBAT Low Limit", Group: "Test Items", Order: 202)]
        [Unit("A")]
        [EnabledIf("meas_ibat", true, HideIfDisabled = true)]
        public double ibat_low_limit { get; set; }
        #endregion

        #region ICUSTOM1
        [Display(Name: "Measure ICUSTOM1", Group: "Test Items", Order: 300)]
        public bool meas_icustom1 { get; set; }

        [Display(Name: "ICUSTOM1 High Limit", Group: "Test Items", Order: 301)]
        [Unit("A")]
        [EnabledIf("meas_icustom1", true, HideIfDisabled = true)]
        public double icustom1_high_limit { get; set; }

        [Display(Name: "ICUSTOM1 Low Limit", Group: "Test Items", Order: 302)]
        [Unit("A")]
        [EnabledIf("meas_icustom1", true, HideIfDisabled = true)]
        public double icustom1_low_limit { get; set; }
        #endregion

        #region ICUSTOM2
        [Display(Name: "Measure ICUSTOM2", Group: "Test Items", Order: 400)]
        public bool meas_icustom2 { get; set; }

        [Display(Name: "ICUSTOM2 High Limit", Group: "Test Items", Order: 401)]
        [Unit("A")]
        [EnabledIf("meas_icustom2", true, HideIfDisabled = true)]
        public double icustom2_high_limit { get; set; }

        [Display(Name: "ICUSTOM2 Low Limit", Group: "Test Items", Order: 402)]
        [Unit("A")]
        [EnabledIf("meas_icustom2", true, HideIfDisabled = true)]
        public double icustom2_low_limit { get; set; }
        #endregion

        #region ITOTAL
        [Display(Name: "Measure ITOTAL", Group: "Test Items", Order: 500)]
        public bool meas_itotal { get; set; }

        [Display(Name: "ITOTAL High Limit", Group: "Test Items", Order: 501)]
        [Unit("A")]
        [EnabledIf("meas_itotal", true, HideIfDisabled = true)]
        public double itotal_high_limit { get; set; }

        [Display(Name: "ITOTAL Low Limit", Group: "Test Items", Order: 502)]
        [Unit("A")]
        [EnabledIf("meas_itotal", true, HideIfDisabled = true)]
        public double itotal_low_limit { get; set; }
        #endregion

        #endregion

        public PA_MeasureCurrent()
        {
            meas_icc = true;
            icc_high_limit = 1;
            icc_low_limit = 0;

            meas_ibat = false;
            ibat_high_limit = 1;
            ibat_low_limit = 0;

            meas_icustom1 = false;
            icustom1_high_limit = 1;
            icustom1_low_limit = 0;

            meas_icustom2 = false;
            icustom2_high_limit = 1;
            icustom2_low_limit = 0;

            meas_itotal = false;
            itotal_high_limit = 1;
            itotal_low_limit = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            PlugInMeasCommon measComm = GetInstrument().MeasCommon;
            PlugInMeas meas = GetInstrument().Measurement;
            Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
            bool ret = true;

            MeasureOrNot(meas_icc, icc_high_limit, icc_low_limit, TestItem_Enum.ICC, tps);
            MeasureOrNot(meas_ibat, ibat_high_limit, ibat_low_limit, TestItem_Enum.IBAT, tps);
            MeasureOrNot(meas_icustom1, icustom1_high_limit, icustom1_low_limit, TestItem_Enum.ICUSTOM1, tps);
            MeasureOrNot(meas_icustom2, icustom2_high_limit, icustom2_low_limit, TestItem_Enum.ICUSTOM2, tps);
            MeasureOrNot(meas_itotal, itotal_high_limit, itotal_low_limit, TestItem_Enum.ITOTAL, tps);

            if (meas_ibat || meas_itotal)
                _meas_type |= DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VBAT;

            if (meas_icc || meas_itotal)
                _meas_type |= DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VCC;

            if (meas_icustom1 || meas_itotal)
                _meas_type |= DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_1;

            if (meas_icustom2 || meas_itotal)
                _meas_type |= DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_2;

            double range = 0.0;
            switch (sense_range)
            {
                default:
                case SENSE_RANGE.RANGE_3A:
                    range = 3.0;
                    break;
                case SENSE_RANGE.RANGE_1mA:
                    range = 0.001;
                    break;
                case SENSE_RANGE.RANGE_100uA:
                    range = 0.0001;
                    break;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            ret = measComm.MeasureCurrent(_meas_type, range, tps);
            sw.Stop();

            PublishResult2(meas_icc, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, icc_high_limit, icc_low_limit, TestItem_Enum.ICC, tps, Results);
            PublishResult2(meas_ibat, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, ibat_high_limit, ibat_low_limit, TestItem_Enum.IBAT, tps, Results);
            PublishResult2(meas_icustom1, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, icustom1_high_limit, icustom1_low_limit, TestItem_Enum.ICUSTOM1, tps, Results);
            PublishResult2(meas_icustom2, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, icustom2_high_limit, icustom2_low_limit, TestItem_Enum.ICUSTOM2, tps, Results);
            PublishResult2(meas_itotal, GetInstrument().selected_technology, GetInstrument().selected_frequency, GetInstrument().applied_target_pout, itotal_high_limit, itotal_low_limit, TestItem_Enum.ITOTAL, tps, Results);

            PA_LOG(PA_LOG_TRS, "CURR ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, MeasureCurrent(ICC:{3,9:F4}mA, IBAT:{4,9:F4}mA, ICUSTOM1:{5,9:F4}mA, ICUSTOM2:{6,9:F4}mA, ITOTAL:{7,9:F4}mA), Time:{8,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  meas_icc ? (from r in tps where r.Key == TestItem_Enum.ICC select r.Value).FirstOrDefault().TestResult * 1e3 : Double.NaN,
                  meas_ibat ? (from r in tps where r.Key == TestItem_Enum.IBAT select r.Value).FirstOrDefault().TestResult * 1e3 : Double.NaN,
                  meas_icustom1 ? (from r in tps where r.Key == TestItem_Enum.ICUSTOM1 select r.Value).FirstOrDefault().TestResult * 1e3 : Double.NaN,
                  meas_icustom2 ? (from r in tps where r.Key == TestItem_Enum.ICUSTOM2 select r.Value).FirstOrDefault().TestResult * 1e3 : Double.NaN,
                  meas_itotal ? (from r in tps where r.Key == TestItem_Enum.ITOTAL select r.Value).FirstOrDefault().TestResult * 1e3 : Double.NaN,
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            if (ret)
                UpgradeVerdict(Verdict.Pass);
            else
                UpgradeVerdict(Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    [Display(Name: "IM3", Groups: new string[] { "PA", "Measurements" })]
    [Description("IM3")]
    public class PA_IM3Measurement : PA_TestStep
    {
        #region Settings
        [Display(Name: "Waveform Format", Order: 8)]
        [Description("The waveform format of selected technology")]
        public IM3_WAVEFORM_FORMAT im3_waveform_format { get; set; }

        [Display(Name: "Modulation Bandwidth", Order: 9)]
        [Description("The bandwidth of modulation signal")]
        [Unit("MHz")]
        [EnabledIf("im3_waveform_format", IM3_WAVEFORM_FORMAT.CW_AND_MODULATION, HideIfDisabled = true)]
        public double dBandWidth { get; set; }

        [Display(Name: "Tone Spacing", Order: 10)]
        [Description("The frequency Spacing of fundamental tones or CW/modulation signal")]
        [Unit("MHz")]
        public double dToneSpace { get; set; }

        [Display(Name: "Span", Order: 11)]
        [Description("Span of Receiver")]
        [Unit("MHz")]
        [EnabledIf("im3_waveform_format", IM3_WAVEFORM_FORMAT.CW_TWO_TONE, HideIfDisabled = true)]
        public double dSpan { get; set; }

        [Display(Name: "RBW", Order: 12)]
        [Description("Resolution bandwidth of receiver")]
        [Unit("KHz")]
        [EnabledIf("im3_waveform_format", IM3_WAVEFORM_FORMAT.CW_TWO_TONE, HideIfDisabled = true)]
        public double dRBW { get; set; }

        [Display(Name: "Filter BW", Order: 13)]
        [Description("Channel filter bandwidth of receiver")]
        [Unit("MHz")]
        [EnabledIf("im3_waveform_format", IM3_WAVEFORM_FORMAT.CW_AND_MODULATION, HideIfDisabled = true)]
        public double dFilterBW { get; set; }

        [Display(Name: "IM3 Optimization Mode", Order: 14)]
        [Description("Turn IM3 optimization mode to be On or Off for VXT")]
        public bool bIM3OptMode { get; set; }

        [Display(Name: "IM3 Result Format", Group: "Result Format", Order: 30)]
        public IM3_RESULT_FORMAT im3_result_format { get; set; }

        #region IM3 Low
        [Display(Name: "Measure IM3 (Low Part)", Group: "Test Items", Order: 100)]
        public bool meas_im3_low { get; set; }

        [Display(Name: "IM3(Low Part) High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_im3_low", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IMD3, HideIfDisabled = true)]
        public double im3_low_high_limit { get; set; }

        [Display(Name: "IM3(Low Part) Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_im3_low", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IMD3, HideIfDisabled = true)]
        public double im3_low_low_limit { get; set; }

        [Display(Name: "IIP3(Low Part) High Limit", Group: "Test Items", Order: 101)]
        [Unit("dBm")]
        [EnabledIf("meas_im3_low", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IIP3, HideIfDisabled = true)]
        public double iip3_low_high_limit { get; set; }

        [Display(Name: "IIP3(Low Part) Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dBm")]
        [EnabledIf("meas_im3_low", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IIP3, HideIfDisabled = true)]
        public double iip3_low_low_limit { get; set; }
        #endregion

        #region IM3 High
        [Display(Name: "Measure IM3 (High Part)", Group: "Test Items", Order: 110)]
        public bool meas_im3_high { get; set; }

        [Display(Name: "IM3(High Part) High Limit", Group: "Test Items", Order: 111)]
        [Unit("dB")]
        [EnabledIf("meas_im3_high", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IMD3, HideIfDisabled = true)]
        public double im3_high_high_limit { get; set; }

        [Display(Name: "IM3(High Part) Low Limit", Group: "Test Items", Order: 112)]
        [Unit("dB")]
        [EnabledIf("meas_im3_high", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IMD3, HideIfDisabled = true)]
        public double im3_high_low_limit { get; set; }

        [Display(Name: "IIP3(High Part) High Limit", Group: "Test Items", Order: 111)]
        [Unit("dBm")]
        [EnabledIf("meas_im3_high", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IIP3, HideIfDisabled = true)]
        public double iip3_high_high_limit { get; set; }

        [Display(Name: "IIP3(High Part) Low Limit", Group: "Test Items", Order: 112)]
        [Unit("dBm")]
        [EnabledIf("meas_im3_high", true, HideIfDisabled = true)]
        [EnabledIf("im3_result_format", IM3_RESULT_FORMAT.IIP3, HideIfDisabled = true)]
        public double iip3_high_low_limit { get; set; }
        #endregion

        #endregion

        public PA_IM3Measurement()
        {
            meas_im3_low = true;
            im3_low_high_limit = 100;
            im3_low_low_limit = -100;
            iip3_low_high_limit = 90;
            iip3_low_low_limit = -90;
            dBandWidth = 5.0;
            dToneSpace = 20.0;
            dSpan = 80.0;
            dRBW = 100.0;
            dFilterBW = 4.5;
            bIM3OptMode = true;

            meas_im3_high = true;
            im3_high_high_limit = 100;
            im3_high_low_limit = -100;
            iip3_high_high_limit = 90;
            iip3_high_low_limit = -90;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool pass = true;

            try
            {
                if (im3_waveform_format == IM3_WAVEFORM_FORMAT.CW_TWO_TONE)
                {
                    // ToDo: Add test case code here
                    PlugInMeas meas = GetInstrument().Measurement;
                    double[] rawData = new double[6];
                    Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                    if (im3_result_format == IM3_RESULT_FORMAT.IMD3)
                    {
                        MeasureOrNot(meas_im3_low, im3_low_high_limit, im3_low_low_limit, TestItem_Enum.IMD3_LOW, tps);
                        MeasureOrNot(meas_im3_high, im3_high_high_limit, im3_high_low_limit, TestItem_Enum.IMD3_HIGH, tps);
                    }
                    else
                    {
                        MeasureOrNot(meas_im3_low, iip3_low_high_limit, iip3_low_low_limit, TestItem_Enum.IIP3_LOW, tps);
                        MeasureOrNot(meas_im3_high, iip3_high_high_limit, iip3_high_low_limit, TestItem_Enum.IIP3_HIGH, tps);
                    }

                    Stopwatch sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    pass = meas.MeasureIM3(tps, dToneSpace, dSpan, dRBW, bIM3OptMode, im3_result_format, ref rawData,
                        GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                    sw.Stop();

                    if (im3_result_format == IM3_RESULT_FORMAT.IMD3)
                    {
                        PublishResult3(meas_im3_low, GetInstrument().selected_technology, GetInstrument().selected_frequency, dToneSpace, im3_low_high_limit, im3_low_low_limit, TestItem_Enum.IMD3_LOW, tps, true, rawData, Results);
                        PublishResult3(meas_im3_high, GetInstrument().selected_technology, GetInstrument().selected_frequency, dToneSpace, im3_high_high_limit, im3_high_low_limit, TestItem_Enum.IMD3_HIGH, tps, false, rawData, Results);

                        PA_LOG(PA_LOG_TRS, "IMD3 ", "[{0}] {1,12}, Freq:{2,5:F0}/{3,5:F0}MHz, PAIn:{4,9:F4}dBm, LeftIM3:{6,9:F4}dBm, LeftTone:{7,9:F4}dBm, RightTone:{8,9:F4}dBm, RightIM3:{9,9:F4}dBm, IMD3(Low:{10,9:F4}dB, High:{11,9:F4}dB), Time:{12,7:F2}ms",
                          pass ? "Pass" : "Fail",
                          TechnologyName(GetInstrument().selected_technology),
                          GetInstrument().selected_frequency - 0.5 * dToneSpace, GetInstrument().selected_frequency + 0.5 * dToneSpace,
                          rawData[0], rawData[1], rawData[4], rawData[2], rawData[3], rawData[5],
                          meas_im3_low ? (from r in tps where r.Key == TestItem_Enum.IMD3_LOW select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_im3_high ? (from r in tps where r.Key == TestItem_Enum.IMD3_HIGH select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                    }
                    else
                    {
                        PublishResult3(meas_im3_low, GetInstrument().selected_technology, GetInstrument().selected_frequency, dToneSpace, iip3_low_high_limit, iip3_low_low_limit, TestItem_Enum.IIP3_LOW, tps, true, rawData, Results);
                        PublishResult3(meas_im3_high, GetInstrument().selected_technology, GetInstrument().selected_frequency, dToneSpace, iip3_high_high_limit, iip3_high_low_limit, TestItem_Enum.IIP3_HIGH, tps, false, rawData, Results);

                        PA_LOG(PA_LOG_TRS, "IIP3 ", "[{0}] {1,12}, Freq:{2,5:F0}/{3,5:F0}MHz, PAIn:{4,9:F4}dBm, LeftIM3:{6,9:F4}dBm, LeftTone:{7,9:F4}dBm, RightTone:{8,9:F4}dBm, RightIM3:{9,9:F4}dBm, IIP3(Low:{10,9:F4}dBm, High:{11,9:F4}dBm), Time:{12,7:F2}ms",
                              pass ? "Pass" : "Fail",
                              TechnologyName(GetInstrument().selected_technology),
                              GetInstrument().selected_frequency - 0.5 * dToneSpace, GetInstrument().selected_frequency + 0.5 * dToneSpace,
                              rawData[0], rawData[1], rawData[4], rawData[2], rawData[3], rawData[5],
                              meas_im3_low ? (from r in tps where r.Key == TestItem_Enum.IIP3_LOW select r.Value).FirstOrDefault().TestResult : Double.NaN,
                              meas_im3_high ? (from r in tps where r.Key == TestItem_Enum.IIP3_HIGH select r.Value).FirstOrDefault().TestResult : Double.NaN,
                              (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                    }

                    if (pass)
                        UpgradeVerdict(Verdict.Pass);
                    else
                        UpgradeVerdict(Verdict.Fail);
                }

                else
                {
                    // ToDo: Add test case code here
                    PlugInMeas meas = GetInstrument().Measurement;
                    double[] rawData = new double[6];
                    Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                    if (im3_result_format == IM3_RESULT_FORMAT.IMD3)
                    {
                        MeasureOrNot(meas_im3_low, im3_low_high_limit, im3_low_low_limit, TestItem_Enum.IMD3_LOW, tps);
                        MeasureOrNot(meas_im3_high, im3_high_high_limit, im3_high_low_limit, TestItem_Enum.IMD3_HIGH, tps);
                    }
                    else
                    {
                        MeasureOrNot(meas_im3_low, iip3_low_high_limit, iip3_low_low_limit, TestItem_Enum.IIP3_LOW, tps);
                        MeasureOrNot(meas_im3_high, iip3_high_high_limit, iip3_high_low_limit, TestItem_Enum.IIP3_HIGH, tps);
                    }

                    Stopwatch sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    pass = meas.MeasureIM3OfModulation(tps, dBandWidth, dToneSpace, dFilterBW, bIM3OptMode, im3_result_format, ref rawData,
                        GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                    sw.Stop();


                    if (im3_result_format == IM3_RESULT_FORMAT.IMD3)
                    {
                        PublishResult3_Modulation(meas_im3_low, GetInstrument().selected_technology, GetInstrument().selected_frequency, dBandWidth, dToneSpace, im3_low_high_limit, im3_low_low_limit, TestItem_Enum.IMD3_LOW, tps, true, rawData, Results);
                        PublishResult3_Modulation(meas_im3_high, GetInstrument().selected_technology, GetInstrument().selected_frequency, dBandWidth, dToneSpace, im3_high_high_limit, im3_high_low_limit, TestItem_Enum.IMD3_HIGH, tps, false, rawData, Results);

                        PA_LOG(PA_LOG_TRS, "IMD3 ", "[{0}] {1,12}, Freq:{2}MHz/ {3}-{4}MHz, PAIn:{5,9:F4}dBm, LeftIM3:{7,9:F4}dBm, LeftTone:{8,9:F4}dBm, RightModulation:{9,9:F4}dBm, RightIM3:{10,9:F4}dBm, IMD3(Low:{11,9:F4}dB, High:{12,9:F4}dB), Time:{13,7:F2}ms",
                          pass ? "Pass" : "Fail",
                          TechnologyName(GetInstrument().selected_technology),
                          GetInstrument().selected_frequency - 0.5 * dToneSpace,
                          GetInstrument().selected_frequency + 0.5 * dToneSpace - dBandWidth / 2, GetInstrument().selected_frequency + 0.5 * dToneSpace + dBandWidth / 2,
                          rawData[0], rawData[1], rawData[4], rawData[2], rawData[3], rawData[5],
                          meas_im3_low ? (from r in tps where r.Key == TestItem_Enum.IMD3_LOW select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          meas_im3_high ? (from r in tps where r.Key == TestItem_Enum.IMD3_HIGH select r.Value).FirstOrDefault().TestResult : Double.NaN,
                          (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                    }
                    else
                    {
                        PublishResult3_Modulation(meas_im3_low, GetInstrument().selected_technology, GetInstrument().selected_frequency, dBandWidth, dToneSpace, iip3_low_high_limit, iip3_low_low_limit, TestItem_Enum.IIP3_LOW, tps, true, rawData, Results);
                        PublishResult3_Modulation(meas_im3_high, GetInstrument().selected_technology, GetInstrument().selected_frequency, dBandWidth, dToneSpace, iip3_high_high_limit, iip3_high_low_limit, TestItem_Enum.IIP3_HIGH, tps, false, rawData, Results);

                        PA_LOG(PA_LOG_TRS, "IIP3 ", "[{0}] {1,12}, Freq:{2}MHz/ {3}-{4}MHz, PAIn:{5,9:F4}dBm, LeftIM3:{7,9:F4}dBm, LeftTone:{8,9:F4}dBm, RightModulation:{9,9:F4}dBm, RightIM3:{10,9:F4}dBm, IIP3(Low:{11,9:F4}dBm, High:{12,9:F4}dBm), Time:{13,7:F2}ms",
                              pass ? "Pass" : "Fail",
                              TechnologyName(GetInstrument().selected_technology),
                              GetInstrument().selected_frequency - 0.5 * dToneSpace,
                              GetInstrument().selected_frequency + 0.5 * dToneSpace - dBandWidth / 2, GetInstrument().selected_frequency + 0.5 * dToneSpace + dBandWidth / 2,
                              rawData[0], rawData[1], rawData[4], rawData[2], rawData[3], rawData[5],
                              meas_im3_low ? (from r in tps where r.Key == TestItem_Enum.IIP3_LOW select r.Value).FirstOrDefault().TestResult : Double.NaN,
                              meas_im3_high ? (from r in tps where r.Key == TestItem_Enum.IIP3_HIGH select r.Value).FirstOrDefault().TestResult : Double.NaN,
                              (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                    }

                    if (pass)
                        UpgradeVerdict(Verdict.Pass);
                    else
                        UpgradeVerdict(Verdict.Fail);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    //[Display(Name: "S-Parameter", Groups: new string[] { "PA", "Measurements" })]
    //[Description("SParameters")]
    //public class PA_SParametersMeasurement : PA_TestStep
    //{
    //    #region Settings
    //    public enum SPARA_SETUP_MODE
    //    {
    //        LOAD_FROM_FILE = 0,
    //        USER_DEFINED = 1
    //    }

    //    [Display(Name: "S-Para Setup Mode", Group: "SPara Setup", Order: 1)]
    //    [Browsable(false)]
    //    public SPARA_SETUP_MODE spara_setup_mode { get; set; }

    //    [Display(Name: "S-Para Setup File", Group: "SPara Setup", Order: 2)]
    //    [FilePath]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.LOAD_FROM_FILE, HideIfDisabled = true)]
    //    public string vna_setup_file { get; set; }

    //    //[Display(Name: "S-Para Setup File", Group: "SPara Setup", Order: 12)]
    //    //[FilePath]
    //    //[EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.LOAD_FROM_FILE, HideIfDisabled = true)]
    //    //public string spara_setup_file { get; set; }

    //    [Display(Name: "Number of Trace", Group: "Trace Number", Order: 21)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public numTrace_Enum num_Trace { get; set; }

    //    //[Display(Name: "Trace1", Description: "Trace name", Groups: new string[] { "Traces" }, Order: 3)]
    //    //[EnabledIf("vna_setup_mode", VNA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    //public string name { get; set; }

    //    #region Trace 1
    //    [Display(Name: "Measurement", Group: "Trace 1", Order: 31)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public measurement_Enum measurementType1 { get; set; }

    //    [Display(Name: "Format", Group: "Trace 1", Order: 32)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public Format_Enum format1 { get; set; }

    //    [Display(Name: "High Limit", Group: "Trace 1", Order: 33)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double HighLimit1 { get; set; }

    //    [Display(Name: "Low Limit", Group: "Trace 1", Order: 34)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double LowLimit1 { get; set; }

    //    #endregion

    //    #region Trace 2
    //    [Display(Name: "Measurement", Group: "Trace 2", Order: 41)]
    //    [EnabledIf("num_Trace", numTrace_Enum.TWO_TRACE, numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public measurement_Enum measurementType2 { get; set; }

    //    [Display(Name: "Format", Group: "Trace 2", Order: 42)]
    //    [EnabledIf("num_Trace", numTrace_Enum.TWO_TRACE, numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public Format_Enum format2 { get; set; }

    //    [Display(Name: "High Limit", Group: "Trace 2", Order: 43)]
    //    [EnabledIf("num_Trace", numTrace_Enum.TWO_TRACE, numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double HighLimit2 { get; set; }

    //    [Display(Name: "Low Limit", Group: "Trace 2", Order: 44)]
    //    [EnabledIf("num_Trace", numTrace_Enum.TWO_TRACE, numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double LowLimit2 { get; set; }

    //    #endregion

    //    #region Trace 3
    //    [Display(Name: "Measurement", Group: "Trace 3", Order: 51)]
    //    [EnabledIf("num_Trace", numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public measurement_Enum measurementType3 { get; set; }

    //    [Display(Name: "Format", Group: "Trace 3", Order: 52)]
    //    [EnabledIf("num_Trace", numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public Format_Enum format3 { get; set; }

    //    [Display(Name: "High Limit", Group: "Trace 3", Order: 53)]
    //    [EnabledIf("num_Trace", numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double HighLimit3 { get; set; }

    //    [Display(Name: "Low Limit", Group: "Trace 3", Order: 54)]
    //    [EnabledIf("num_Trace", numTrace_Enum.THREE_TRACE, numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double LowLimit3 { get; set; }

    //    #endregion

    //    #region Trace 4
    //    [Display(Name: "Measurement", Group: "Trace 4", Order: 61)]
    //    [EnabledIf("num_Trace", numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public measurement_Enum measurementType4 { get; set; }

    //    [Display(Name: "Format", Group: "Trace 4", Order: 62)]
    //    [EnabledIf("num_Trace", numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public Format_Enum format4 { get; set; }

    //    [Display(Name: "High Limit", Group: "Trace 4", Order: 63)]
    //    [EnabledIf("num_Trace", numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double HighLimit4 { get; set; }

    //    [Display(Name: "Low Limit", Group: "Trace 4", Order: 64)]
    //    [EnabledIf("num_Trace", numTrace_Enum.FOUR_TRACE, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double LowLimit4 { get; set; }

    //    #endregion

    //    [Display(Name: "Number of Segment", Group: "Segment Number", Order: 71)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public numSegment_Enum num_Segment { get; set; }

    //    #region Segment 1
    //    [Display(Name: "Start Frequency", Group: "Segment 1", Order: 81)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Start_Freq1 { get; set; }

    //    [Display(Name: "Stop Frequency", Group: "Segment 1", Order: 82)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Stop_Freq1 { get; set; }

    //    [Display(Name: "Number Points", Group: "Segment 1", Order: 83)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public int numPoints1 { get; set; }

    //    [Display(Name: "IF Bandwidth", Group: "Segment 1", Order: 84)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double if_bandwidth1 { get; set; }

    //    [Display(Name: "Power", Group: "Segment 1", Order: 85)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double power1 { get; set; }
    //    #endregion

    //    #region Segment 2
    //    [Display(Name: "Start Frequency", Group: "Segment 2", Order: 91)]
    //    [EnabledIf("num_Segment", numSegment_Enum.TWO_SEGMENT, numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Start_Freq2 { get; set; }

    //    [Display(Name: "Stop Frequency", Group: "Segment 2", Order: 92)]
    //    [EnabledIf("num_Segment", numSegment_Enum.TWO_SEGMENT, numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Stop_Freq2 { get; set; }

    //    [Display(Name: "Number Points", Group: "Segment 2", Order: 93)]
    //    [EnabledIf("num_Segment", numSegment_Enum.TWO_SEGMENT, numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public int numPoints2 { get; set; }

    //    [Display(Name: "IF Bandwidth", Group: "Segment 2", Order: 94)]
    //    [EnabledIf("num_Segment", numSegment_Enum.TWO_SEGMENT, numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double if_bandwidth2 { get; set; }

    //    [Display(Name: "Power", Group: "Segment 2", Order: 95)]
    //    [EnabledIf("num_Segment", numSegment_Enum.TWO_SEGMENT, numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double power2 { get; set; }
    //    #endregion

    //    #region Segment 3
    //    [Display(Name: "Start Frequency", Group: "Segment 3", Order: 101)]
    //    [EnabledIf("num_Segment", numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Start_Freq3 { get; set; }

    //    [Display(Name: "Stop Frequency", Group: "Segment 3", Order: 102)]
    //    [EnabledIf("num_Segment", numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Stop_Freq3 { get; set; }

    //    [Display(Name: "Number Points", Group: "Segment 3", Order: 103)]
    //    [EnabledIf("num_Segment", numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public int numPoints3 { get; set; }

    //    [Display(Name: "IF Bandwidth", Group: "Segment 3", Order: 104)]
    //    [EnabledIf("num_Segment", numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double if_bandwidth3 { get; set; }

    //    [Display(Name: "Power", Group: "Segment 3", Order: 105)]
    //    [EnabledIf("num_Segment", numSegment_Enum.THREE_SEGMENT, numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double power3 { get; set; }
    //    #endregion

    //    #region Segment 4
    //    [Display(Name: "Start Frequency", Group: "Segment 4", Order: 111)]
    //    [EnabledIf("num_Segment", numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Start_Freq4 { get; set; }

    //    [Display(Name: "Stop Frequency", Group: "Segment 4", Order: 112)]
    //    [EnabledIf("num_Segment", numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double Stop_Freq4 { get; set; }

    //    [Display(Name: "Number Points", Group: "Segment 4", Order: 113)]
    //    [EnabledIf("num_Segment", numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public int numPoints4 { get; set; }

    //    [Display(Name: "IF Bandwidth", Group: "Segment 4", Order: 114)]
    //    [EnabledIf("num_Segment", numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double if_bandwidth4 { get; set; }

    //    [Display(Name: "Power", Group: "Segment 4", Order: 115)]
    //    [EnabledIf("num_Segment", numSegment_Enum.FOUR_SEGMENT, HideIfDisabled = true)]
    //    [EnabledIf("spara_setup_mode", SPARA_SETUP_MODE.USER_DEFINED, HideIfDisabled = true)]
    //    public double power4 { get; set; }
    //    #endregion

    //    #endregion

    //    public PA_SParametersMeasurement()
    //    {
    //        spara_setup_mode = SPARA_SETUP_MODE.LOAD_FROM_FILE;

    //        num_Trace = numTrace_Enum.ONE_TRACE;

    //        measurementType1 = measurement_Enum.S11;
    //        format1 = Format_Enum.SMITH;
    //        HighLimit1 = 1.0;
    //        LowLimit1 = 0.0;

    //        measurementType2 = measurement_Enum.S21;
    //        format2 = Format_Enum.MLOG;
    //        HighLimit2 = 1.0;
    //        LowLimit2 = 0.0;

    //        measurementType3 = measurement_Enum.S12;
    //        format3 = Format_Enum.SMITH;
    //        HighLimit3 = 1.0;
    //        LowLimit3 = 0.0;

    //        measurementType4 = measurement_Enum.S22;
    //        format4 = Format_Enum.SMITH;
    //        HighLimit4 = 1.0;
    //        LowLimit4 = 0.0;

    //        num_Segment = numSegment_Enum.ONE_SEGMENT;

    //        Start_Freq1 = 7.0e8;
    //        Stop_Freq1 = 9.5e8;
    //        numPoints1 = 401;
    //        if_bandwidth1 = 1e5;
    //        power1 = 0.0;

    //        Start_Freq2 = 1.5e9;
    //        Stop_Freq2 = 1.9e9;
    //        numPoints2 = 201;
    //        if_bandwidth2 = 1e5;
    //        power2 = 0.0;

    //        Start_Freq3 = 1.9e9;
    //        Stop_Freq3 = 2.1e9;
    //        numPoints3 = 401;
    //        if_bandwidth3 = 1e5;
    //        power3 = 0.0;

    //        Start_Freq4 = 2.1e9;
    //        Stop_Freq4 = 2.5e9;
    //        numPoints4 = 201;
    //        if_bandwidth4 = 1e5;
    //        power4 = 0.0;


    //    }

    //    public override void PrePlanRun()
    //    {
    //        base.PrePlanRun();
    //        // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
    //    }

    //    public override void Run()
    //    {
    //        bool pass = true;
    //        bool ret = true;

    //        bool[] test_result = new bool[4];

    //        for (int i = 0; i < 4; i++)
    //        {
    //            test_result[i] = true;
    //        }

    //        try
    //        {
    //            // ToDo: Add test case code here
    //            PlugInMeas meas = GetInstrument().Measurement;
    //            IVNA vna = GetInstrument().vna;

    //            SparaConfigure SparaConfig = new SparaConfigure();
    //            SparaConfig.measurementType = new measurement_Enum[4];
    //            SparaConfig.format = new Format_Enum[4];
    //            SparaConfig.HighLimit = new double[4];
    //            SparaConfig.LowLimit = new double[4];

    //            SparaConfig.Start_Freq = new double[4];
    //            SparaConfig.Stop_Freq = new double[4];
    //            SparaConfig.numPoints = new int[4];
    //            SparaConfig.if_bandwidth = new double[4];
    //            SparaConfig.power = new double[4];

    //            if (spara_setup_mode == SPARA_SETUP_MODE.USER_DEFINED)
    //            {
    //                SparaConfig.num_Trace = num_Trace;
    //                SparaConfig.measurementType[0] = measurementType1;
    //                SparaConfig.measurementType[1] = measurementType2;
    //                SparaConfig.measurementType[2] = measurementType3;
    //                SparaConfig.measurementType[3] = measurementType4;
    //                SparaConfig.format[0] = format1;
    //                SparaConfig.format[1] = format2;
    //                SparaConfig.format[2] = format3;
    //                SparaConfig.format[3] = format4;
    //                SparaConfig.HighLimit[0] = HighLimit1;
    //                SparaConfig.HighLimit[1] = HighLimit2;
    //                SparaConfig.HighLimit[2] = HighLimit3;
    //                SparaConfig.HighLimit[3] = HighLimit4;

    //                SparaConfig.LowLimit[0] = LowLimit1;
    //                SparaConfig.LowLimit[1] = LowLimit2;
    //                SparaConfig.LowLimit[2] = LowLimit3;
    //                SparaConfig.LowLimit[3] = LowLimit4;

    //                SparaConfig.num_Segment = num_Segment;
    //                SparaConfig.Start_Freq[0] = Start_Freq1;
    //                SparaConfig.Start_Freq[1] = Start_Freq2;
    //                SparaConfig.Start_Freq[2] = Start_Freq3;
    //                SparaConfig.Start_Freq[3] = Start_Freq4;
    //                SparaConfig.Stop_Freq[0] = Stop_Freq1;
    //                SparaConfig.Stop_Freq[1] = Stop_Freq2;
    //                SparaConfig.Stop_Freq[2] = Stop_Freq3;
    //                SparaConfig.Stop_Freq[3] = Stop_Freq4;
    //                SparaConfig.numPoints[0] = numPoints1;
    //                SparaConfig.numPoints[1] = numPoints2;
    //                SparaConfig.numPoints[2] = numPoints3;
    //                SparaConfig.numPoints[3] = numPoints4;
    //                SparaConfig.if_bandwidth[0] = if_bandwidth1;
    //                SparaConfig.if_bandwidth[1] = if_bandwidth2;
    //                SparaConfig.if_bandwidth[2] = if_bandwidth3;
    //                SparaConfig.if_bandwidth[3] = if_bandwidth4;
    //                SparaConfig.power[0] = power1;
    //                SparaConfig.power[1] = power2;
    //                SparaConfig.power[2] = power3;
    //                SparaConfig.power[3] = power4;

    //                meas.passVnaSetupData(SparaConfig);

    //                ret = vna.configSparms();
    //                PA_LOG(PA_LOG_TC, "VNA  ", "Config S Parameter {0}!", ret ? "Pass" : "Fail");
    //            }
    //            else
    //            {
    //                ret = vna.readVnaSetupData(vna_setup_file);
    //                PA_LOG(PA_LOG_TC, "VNA  ", "Read VNA SetupData from File {0} {0}!", vna_setup_file, ret ? "Pass" : "Fail");
    //                ret = vna.configSparms();
    //                PA_LOG(PA_LOG_TC, "VNA  ", "Config S Parameter {0}!", ret ? "Pass" : "Fail");
    //            }

    //            Stopwatch sw = new Stopwatch();
    //            sw.Reset();
    //            sw.Start();
    //            meas.MeasureSParams(ref test_result);
    //            sw.Stop();

    //            pass = test_result[0] && test_result[1] && test_result[2] && test_result[3];

    //            PA_LOG(PA_LOG_TRS, "S-PAR", "[{0}] (Segment1:{1}, Segment2:{2}, Segment3:{3}, Segment3:{4}), Time:{5,7:F2}ms",
    //                  pass ? "Pass" : "Fail",
    //                  test_result[0] ? "Pass" : "Fail",
    //                  test_result[1] ? "Pass" : "Fail",
    //                  test_result[2] ? "Pass" : "Fail",
    //                  test_result[3] ? "Pass" : "Fail",
    //                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

    //            if (pass)
    //                UpgradeVerdict(Verdict.Pass);
    //            else
    //                UpgradeVerdict(Verdict.Fail);
    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Error("Exception {0} !", ex.ToString());
    //            UpgradeVerdict(Verdict.Fail);
    //        }
    //    }

    //    public override void PostPlanRun()
    //    {
    //        base.PostPlanRun();
    //        // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
    //    }

    //}

    [Display(Name: "S-Parameter", Groups: new string[] { "PA", "Measurements" })]
    [Description("SParameters")]
    public class PA_SParametersMeasurement : PA_TestStep
    {
        private bool vnaRecallStateFile = false;
        private bool vnaSaveTraceEnabled = false;
        private bool vnaSaveSnpEnabled = false;

        #region Settings
        [Display(Name: "Setup File Recalled", Group: "SPara Setup", Order: 1)]
        public bool recall_setup_data
        {
            get { return vnaRecallStateFile; }
            set
            {
                vnaRecallStateFile = value;
            }
        }

        [Display(Name: "VNA Setup File", Group: "SPara Setup", Order: 2)]
        [EnabledIf("recall_setup_data", true, HideIfDisabled = true)]
        [FilePath]
        public string vna_setup_file { get; set; }

        [Display(Name: "Active Channel Num", Group: "Channel", Order: 3)]
        public int vna_chanNum
        {
            get
            {
                return _vna_chanNum;
            }
            set
            {
                _vna_chanNum = value;
                bool bGenerateError = false;
                if (_iPre_vna_ChanNum != _vna_chanNum)
                    bGenerateError = true;

                if ((pa_instrument != null) && GetInstrument().bVnaInitialized)
                {
                    if (_vna_chanNum > GetInstrument().iVnaNumChannel && bGenerateError)
                    {
                        Log.Error("Input active channel number exceeds maximum channel number");
                    }
                    else if (_vna_chanNum == 0 && bGenerateError)
                    {
                        Log.Error("Active channel number cannot equal 0");
                    }
                }
                _iPre_vna_ChanNum = _vna_chanNum;
            }
        }
        private int _vna_chanNum;
        private int _iPre_vna_ChanNum;

        [Display(Name: "Disable Panel Display", Group: "Soft Front Panel", Order: 4)]
        public bool isVnaInvisible { get; set; }

        [Display(Name: "Save Trace Data", Group: "Export Log", Order: 5)]
        public bool save_trace
        {
            get { return vnaSaveTraceEnabled; }
            set
            {
                vnaSaveTraceEnabled = value;
            }
        }

        [Display(Name: "Save Snp Data", Group: "Export Log", Order: 6)]
        public bool save_snp
        {
            get { return vnaSaveSnpEnabled; }
            set
            {
                vnaSaveSnpEnabled = value;
            }
        }
        #endregion


        public PA_SParametersMeasurement()
        {
            save_trace = true;
            save_snp = true;
            vna_chanNum = 1;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            PlugInMeas meas = GetInstrument().Measurement;
            IVNA vna = GetInstrument().vna;
            bool pass = true;

            if (vnaRecallStateFile)
            {
                if (Path.GetExtension(vna_setup_file) == ".csv")
                {
                    pass = vna.readVnaSetupData(vna_setup_file);
                    Log.Info("Read VNA SetupData from File {0} {1}!", vna_setup_file, pass ? "Pass" : "Fail");
                    pass = vna.configSparms();
                    //Log.Info("Config S Parameter {0}!", pass ? "Pass" : "Fail");
                }

                else if (Path.GetExtension(vna_setup_file) == ".csa")
                {
                    pass = vna.loadVnaStateFile(vna_setup_file);
                    Log.Info("Read VNA SetupData from File {0} {1}!", vna_setup_file, pass ? "Pass" : "Fail");
                    pass = vna.setupStateFileData();
                }

            }

            int iNumTrace = 0;
            vna.getNumTraceOfChannel(vna_chanNum, ref iNumTrace, ref pass);
            if (pass == false)
            {
                Log.Error("Input active channel number is invalid.");

                PA_LOG(PA_LOG_TRS, "VNA  ", "[{0}] S parameter measurement failure.", pass ? "Pass" : "Fail");
                UpgradeVerdict(Verdict.Fail);

            }
            else
            {
                bool[] test_result = new bool[iNumTrace];

                for (int i = 0; i < iNumTrace; i++)
                {
                    test_result[i] = true;
                }

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    meas.MeasureSParams(vna_chanNum, ref test_result, isVnaInvisible);
                    sw.Stop();

                    for (int i = 0; i < iNumTrace; i++)
                    {
                        pass = pass && test_result[i];
                    }

                    PA_LOG(PA_LOG_TRS, "S-PAR", "[{0}], Time:{1,3:F2}ms", pass ? "Pass" : "Fail", (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
                    for (int i = 0; i < iNumTrace; i++)
                    {
                        PA_LOG(PA_LOG_TRS, "S-PAR", "(Segment{0} [{1}])", (i + 1), test_result[i] ? "Pass" : "Fail");
                    }

                    if (pass)
                        UpgradeVerdict(Verdict.Pass);
                    else
                        UpgradeVerdict(Verdict.Fail);

                    // Export result data through .csv/.snp file
                    if (vnaSaveTraceEnabled)
                    {
                        pass = vna.saveTracesToCSV(vna_chanNum);
                        PA_LOG(PA_LOG_TRS, "VNA  ", "[{0}] VNA CSV Formatted Data Generated!", pass ? "Pass" : "Fail");
                    }

                    if (vnaSaveSnpEnabled)
                    {
                        int vnaPortNum = Convert.ToInt32(GetInstrument().vna_port_num);
                        string[] temp = new string[vnaPortNum];
                        string PortNumbers = "";
                        for (int i = 0; i < vnaPortNum; i++)
                        {
                            temp[i] = (i + 1).ToString();
                        }
                        for (int i = 0; i < vnaPortNum - 1; i++)
                        {
                            PortNumbers += temp[i] + ",";
                        }
                        PortNumbers += temp[vnaPortNum - 1];

                        pass = vna.saveTracesToSnP(vna_chanNum, "s" + vnaPortNum.ToString() + "p", PortNumbers);
                        PA_LOG(PA_LOG_TRS, "VNA  ", "[{0}] VNA SnP Data Generated!", pass ? "Pass" : "Fail");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception {0} !", ex.ToString());
                    UpgradeVerdict(Verdict.Fail);
                }
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    [Display(Name: "Noise Power for Rx Path", Groups: new string[] { "PA", "Measurements" })]
    [Description("Noise Power for Rx Path")]
    public class PA_NPRPMeasurement : PA_TestStep
    {
        #region settings
        //should be in range[1~64]
        [Display(Name: "Span", Group: "NPRP", Order: 11)]
        [Unit("MHz")]
        public double dSpan { get; set; }

        [Display(Name: "Steps", Group: "NPRP", Order: 12)]
        public int iSteps { get; set; }

        [Display(Name: "Analyzer Center Frequency", Group: "NPRP", Order: 13)]
        [Unit("MHz")]
        public double dVSACenterFrequency { get; set; }

        [Display(Name: "Source Center Frequency", Group: "NPRP", Order: 14)]
        [Unit("MHz")]
        public double dVSGCenterFrequency { get; set; }

        [Display(Name: "NPRP Limit", Group: "Test Limits", Order: 31)]
        public double nprp_limit { get; set; }

        //[Display(Name: "NPRP Low Limit", Group: "Test Limits", Order: 32)]
        //public double nprp_low_limit { get; set; }

        #endregion

        public PA_NPRPMeasurement()
        {
            dVSGCenterFrequency = 1700;
            dVSACenterFrequency = 1700;
            iSteps = 10;
            dSpan = 64;
            nprp_limit = -65;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool pass = true;
            double time_consumed = 0;

            bool use_cal_file = false;
            double dut_input_cable_loss = 0.0;
            double dut_output_cable_loss = 0.0;

            //temporary using 64 points FFT, because this hardware does not support 128M sample bandwidth, we could change it to 128 points in future
            int FFTLength = 64;

            double[][] SpectrumData = new double[iSteps][];

            for (int i = 0; i < iSteps; i++)
            {
                SpectrumData[i] = new double[FFTLength];
                //SpectrumData[i] = new double[1024];
            }

            try
            {
                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                meas.MeasureNPRP(ref SpectrumData, dSpan, iSteps, dVSACenterFrequency, dVSGCenterFrequency, use_cal_file,
                    dut_input_cable_loss, dut_output_cable_loss,
                    GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);
                sw.Stop();

                for (int i = 0; i < iSteps; i++)
                {
                    for (int j = 0; j < dSpan; j++)
                    {
                        if (SpectrumData[i][j] > nprp_limit)
                        {
                            pass = false;
                            Logging(LogLevel.INFO, "Find out the faluire test point out of limit for step[{0}], frequency[{0}]", i, j);
                        }
                    }
                }


                if (true)
                {
                    //print result
                    for (int i = 0; i < iSteps; i++)
                    {
                        Logging(LogLevel.INFO, "SpectrumData of step[{0}]:", i);
                        for (int j = 0; j < dSpan; j++)
                        {
                            Logging(LogLevel.INFO, " {0}", SpectrumData[i][j].ToString());
                        }
                        Logging(LogLevel.INFO, "\n");
                    }
                }

                PA_LOG(PA_LOG_TRS, "NPRP ", "[{0}] Time:{5,7:F2}ms",
                      pass ? "Pass" : "Fail",
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    //[Display(Name: "Dynamic EVM", Groups: new string[] { "PA", "Measurements" })]
    //[Description("Dynamic EVM for WLAN")]
    //public class PA_DynamicEVMMeasurement : PA_TestStep
    //{
    //    #region Settings
    //    [Display(Name: "Add Noise Reduction", Group: "Noise Reduction", Order: 10)]
    //    [Browsable(true)]
    //    public bool NoiseReductionUsedinDEVM { get; set; }

    //    //[Display(Name: "Pulse Voltage", Group: "DynamicEVM", Order: 11)]
    //    //[Unit("V")]
    //    //public double dPulseVoltage { get; set; }

    //    [Display(Name: "High Limit", Group: "Test Limits", Order: 12)]
    //    [Unit("dB")]
    //    [Browsable(true)]
    //    public double HighLimit_WLAN { get; set; }

    //    [Display(Name: "Low Limit", Group: "Test Limits", Order: 13)]
    //    [Unit("dB")]
    //    [Browsable(true)]
    //    public double LowLimit_WLAN { get; set; }

    //    #endregion

    //    public double[] evmData;
    //    public KmfEvmResultView evmResult = new KmfEvmResultView();

    //    public PA_DynamicEVMMeasurement()
    //    {
    //        //dPulseVoltage = 1.0;
    //        NoiseReductionUsedinDEVM = false;
    //        LowLimit_WLAN = -998;
    //        HighLimit_WLAN = -35;

    //    }

    //    public override void PrePlanRun()
    //    {
    //        base.PrePlanRun();
    //        // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
    //    }

    //    public override void Run()
    //    {
    //        bool pass = true;
    //        double time_consumed = 0;

    //        try
    //        {
    //            // ToDo: Add test case code here
    //            PlugInMeas meas = pa_instrument.Measurement;

    //            Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

    //            int triggerLineNumber = (int)pa_instrument.chassis_trigger_line;

    //            Stopwatch sw = new Stopwatch();
    //            sw.Reset();
    //            sw.Start();

    //            //the pre-condition was configured in selectTechnology Step
    //            ////setup VSG and AWG
    //            //meas.MeasureDynamicEVM(pa_instrument.selected_waveform, pa_instrument.dLeadTime,
    //            //   pa_instrument.dLagTime, pa_instrument.dDutycycle, dPulseVoltage, triggerLineNumber, pa_instrument.dRFTriggerDelay);

    //            //config VSA receiver trigger

    //            //measure EVM result
    //            MeasureOrNot(true, HighLimit_WLAN, LowLimit_WLAN, TestItem_Enum.EVM, tps);
    //            pa_instrument.pluginKmf.evmErrorMsg = string.Empty;

    //            pass = meas.KmfMeasureEVM(NoiseReductionUsedinDEVM, pa_instrument.selected_technology,
    //                                    pa_instrument.selected_frequency * 1e6,
    //                                    search_length,
    //                                    ref tps);
    //            sw.Stop();

    //            PublishResult1(true, pa_instrument.selected_technology, pa_instrument.selected_frequency, HighLimit_WLAN, LowLimit_WLAN, TestItem_Enum.EVM, tps, Results);

    //            if (pa_instrument.selected_technology.ToUpper().Contains("WLAN"))
    //                evmResult.UpdateDB(tps[TestItem_Enum.EVM].TestResult, -999, -999, -999, -999);
    //            else
    //                evmResult.Update(tps[TestItem_Enum.EVM].TestResult, -999, -999, -999, -999);

    //            PA_LOG(PA_LOG_TRS, "D-EVM", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Dynamic EVM:{3,9:F4}{4}, Time:{5,7:F2}ms",
    //                  pass ? "Pass" : "Fail",
    //                  TechnologyName(pa_instrument.selected_technology),
    //                  pa_instrument.selected_frequency,
    //                  (from r in tps where r.Key == TestItem_Enum.EVM select r.Value).FirstOrDefault().TestResult,
    //                  pa_instrument.selected_technology.ToUpper().Contains("WLAN") ? "dB" : "%",
    //                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

    //            if (pass)
    //                UpgradeVerdict(Verdict.Pass);
    //            else
    //                UpgradeVerdict(Verdict.Fail);
    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Error("Exception {0} !", ex.ToString());
    //            UpgradeVerdict(Verdict.Fail);
    //        }
    //    }

    //    public override void PostPlanRun()
    //    {
    //        base.PostPlanRun();
    //        // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
    //    }

    //}

    [Display(Name: "Per-Pin Measurement", Groups: new string[] { "PA", "Measurements" })]
    [Description("Digital IO Per-Pin Measurement")]
    public class PPMU_Measurement : PA_TestStep
    {
        [Browsable(false)]
        public List<string> chanList { get; set; }

        #region Settings

        #region PPMU Channel
        [Display(Name: "Channel Name", Order: 10)]
        [AvailableValues("chanList")]
        public string ppmu_chan_name { get; set; }

        [Display(Name: "Operation", Order: 11)]
        public PPMU_OPERATION ppmu_chan_op { get; set; }

        [Display(Name: "Current", Order: 12)]
        [Unit("A")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_CURRENT,
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_CURRENT,
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_VOLTAGE,
            HideIfDisabled = true)]
        public double ppmu_chan_current { get; set; }

        [Display(Name: "Voltage", Order: 12.5)]
        [Unit("V")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_VOLTAGE,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_VOLTAGE,
            HideIfDisabled = true)]
        public double ppmu_chan_voltage { get; set; }

        [Display(Name: "Current Limit", Order: 13)]
        [Unit("A")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT,
            HideIfDisabled = true)]
        public double ppmu_chan_current_limit { get; set; }


        [Display(Name: "Measured Voltage Upper Limit", Group: "Test Items", Order: 14)]
        [Unit("V")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_NOTHING_MEASURE_VOLTAGE,
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_VOLTAGE,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_VOLTAGE,
            HideIfDisabled = true)]
        public double meas_voltage_upper_limit { get; set; }

        [Display(Name: "Measured Voltage Lower Limit", Group: "Test Items", Order: 15)]
        [Unit("V")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_NOTHING_MEASURE_VOLTAGE,
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_VOLTAGE,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_VOLTAGE,
            HideIfDisabled = true)]
        public double meas_voltage_lower_limit { get; set; }

        [Display(Name: "Measured Current Upper Limit", Group: "Test Items", Order: 16)]
        [Unit("A")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_CURRENT,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT,
            HideIfDisabled = true)]
        public double meas_current_upper_limit { get; set; }

        [Display(Name: "Measured Current Lower Limit", Group: "Test Items", Order: 17)]
        [Unit("A")]
        [EnabledIf("ppmu_chan_op",
            PPMU_OPERATION.FORCE_CURRENT_MEASURE_CURRENT,
            PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT,
            HideIfDisabled = true)]
        public double meas_current_lower_limit { get; set; }

        #endregion

        #endregion

        public PPMU_Measurement()
        {
            if (pa_instrument != null
                && pa_instrument.ppmu_config_file != null
                && File.Exists(pa_instrument.ppmu_config_file)
                && pa_instrument.DioIsM9195())
            {
                chanList = PlugInM9195A.PPMU_ChannLists();
            }
            else
            {
                chanList = null;
            }
            meas_current_lower_limit = 0.0;
            meas_current_upper_limit = 1.0;
            meas_voltage_lower_limit = 0.0;
            meas_voltage_upper_limit = 1.0;
            ppmu_chan_current_limit = 0.04;
            ppmu_chan_voltage = 1.0;
            ppmu_chan_current = 0.01;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret;
            IPPMU ppmu = pa_instrument.ppmu;
          
            PlugInMeasCommon meas = GetInstrument().MeasCommon;
            double result = double.NaN;

            try
            {
                #region Prepare parameters
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                double level = 0.0;
                double limit = 0.0;
                double high = 0.0;
                double low = 0.0;
                TestItem_Enum ti = TestItem_Enum.NONE;
                switch (ppmu_chan_op)
                {
                    case PPMU_OPERATION.FORCE_CURRENT:
                        level = ppmu_chan_current;
                        break;

                    case PPMU_OPERATION.FORCE_VOLTAGE:
                    default:
                        level = ppmu_chan_voltage;
                        break;

                    case PPMU_OPERATION.FORCE_NOTHING_MEASURE_VOLTAGE:
                        MeasureOrNot(true, meas_voltage_upper_limit, meas_voltage_lower_limit, TestItem_Enum.PPMU_VOLT, tps);
                        ti = TestItem_Enum.PPMU_VOLT;
                        high = meas_voltage_upper_limit;
                        low = meas_voltage_lower_limit;
                        break;

                    case PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_VOLTAGE:
                        MeasureOrNot(true, meas_voltage_upper_limit, meas_voltage_lower_limit, TestItem_Enum.PPMU_VOLT, tps);
                        ti = TestItem_Enum.PPMU_VOLT;
                        high = meas_voltage_upper_limit;
                        low = meas_voltage_lower_limit;
                        level = ppmu_chan_voltage;
                        break;

                    case PPMU_OPERATION.FORCE_CURRENT_MEASURE_VOLTAGE:
                        MeasureOrNot(true, meas_voltage_upper_limit, meas_voltage_lower_limit, TestItem_Enum.PPMU_VOLT, tps);
                        ti = TestItem_Enum.PPMU_VOLT;
                        high = meas_voltage_upper_limit;
                        low = meas_voltage_lower_limit;
                        level = ppmu_chan_current;
                        break;

                    case PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT:
                        MeasureOrNot(true, meas_current_upper_limit, meas_current_lower_limit, TestItem_Enum.PPMU_CURR, tps);
                        ti = TestItem_Enum.PPMU_CURR;
                        level = ppmu_chan_voltage;
                        high = meas_current_upper_limit;
                        low = meas_current_lower_limit;
                        limit = ppmu_chan_current_limit;
                        break;

                    case PPMU_OPERATION.FORCE_CURRENT_MEASURE_CURRENT:
                        MeasureOrNot(true, meas_current_upper_limit, meas_current_lower_limit, TestItem_Enum.PPMU_CURR, tps);
                        ti = TestItem_Enum.PPMU_CURR;
                        high = meas_current_upper_limit;
                        low = meas_current_lower_limit;
                        level = ppmu_chan_current;
                        break;

                    case PPMU_OPERATION.HIGH_IMPEDANCE:
                        break;
                }
                #endregion
                ret = meas.MeasurePPMU(ppmu_chan_name, ppmu_chan_op, level, tps, ref result, limit);
                PublishResult4(true, ppmu_chan_name, high, low, ti, tps, Results);
            }

            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                ret = false;
            }

            PA_LOG(PA_LOG_TC, "DIO  ", "[{0}] PPMU Measurement [{1,30}] Done, Result = [{2,9:F6}]!", ret ? "Pass" : "Fail", ppmu_chan_op.ToString(), result);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }


    [Display(Name: "State Transition Analysis", Groups: new string[] { "PA", "Measurements" })]
    [Description("State transition analysis")]
    public class PA_StateTransition : PA_TestStep
    {
        #region Settings

        [Display(Name: "Operation", Group: "State Transition", Order: 01)]
        public FEM_STATE_TRANSITION eFEM_State_Transition { get; set; }

        private double _StateTransition_upper_limit;
        [Display(Name: "State Transition Upper Limit", Group: "Test Items", Order: 02)]
        [Unit("ns")]
        public double StateTransition_upper_limit
        {
            get
            {
                return _StateTransition_upper_limit;
            }
            set
            {
                _StateTransition_upper_limit = value;
                if (_StateTransition_upper_limit > 1000.0)
                {
                    _StateTransition_upper_limit = 1000;
                }
                else if (_StateTransition_upper_limit < 0.0)
                {
                    _StateTransition_upper_limit = 0.0;
                }

                if (_StateTransition_lower_limit > _StateTransition_upper_limit)
                {
                    _StateTransition_upper_limit = _StateTransition_lower_limit;
                }
            }
        }

        private double _StateTransition_lower_limit;
        [Display(Name: "State Transition Lower Limit", Group: "Test Items", Order: 03)]
        [Unit("ns")]
        public double StateTransition_lower_limit
        {
            get
            {
                return _StateTransition_lower_limit;
            }
            set
            {
                _StateTransition_lower_limit = value;
                if (_StateTransition_lower_limit > 1000.0)
                {
                    _StateTransition_lower_limit = 1000;
                }
                else if (_StateTransition_lower_limit < 0.0)
                {
                    _StateTransition_lower_limit = 0.0;
                }

                if (_StateTransition_lower_limit > _StateTransition_upper_limit)
                {
                    _StateTransition_lower_limit = _StateTransition_upper_limit;
                }
            }
        }

        #endregion

        public PA_StateTransition()
        {
            eFEM_State_Transition = FEM_STATE_TRANSITION.TX_to_RXLNA_Transition;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret;
            PlugInMeas meas = GetInstrument().Measurement;
            double result = double.NaN;

            try
            {
                #region Prepare parameters
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                double level = 0.0;
                double limit = 0.0;
                double high = 0.0;
                double low = 0.0;
                TestItem_Enum ti = TestItem_Enum.NONE;
                #endregion

                MeasureOrNot(true, StateTransition_upper_limit, StateTransition_lower_limit, TestItem_Enum.STATE_TRANS, tps);
                ti = TestItem_Enum.STATE_TRANS;
                high = StateTransition_upper_limit;
                low = StateTransition_lower_limit;

                //ret = meas.DIOPatternGen(eFEM_State_Transition);
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                GetInstrument().dio.FEMStateTrans(eFEM_State_Transition);
                ret = meas.MeasureStateTransition(tps, ref result, GetInstrument().use_secondary_source_analyzer, GetInstrument().secondary_source_analyzer_config);

                GetInstrument().dio.PatternSiteWithHiZ();

                PublishResult4(true, "", high, low, ti, tps, Results);

                sw.Stop();

                PA_LOG(PA_LOG_TRS, "STT", "[{0}] {1,12}, Freq:{2,5:F0}MHz, TransTime:{3,9:F4}ns, Time:{4,7:F2}ms",
                      ret ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      result * 1e9,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);
            }

            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                ret = false;
            }

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    #endregion

    #region KMF Measurement

    #region Noise Figure Measurement
    [Display(Name: "Noise Figure Cold Source", Groups: new string[] { "PA", "Measurements" })]
    [Description("Noise Figure Using Cold Source")]
    public class PA_NFColdSourceMeasurement : PA_TestStep
    {
        private string _freqmode;
        #region Settings

        [Display(Name: "Acquisition Duration for Gain Meas", Group: "VSA Config", Order: 1)]
        [Unit("ms")]
        public double dGainMeasDuration { get; set; }

        [Display(Name: "Source Level for Gain Meas", Group: "VSA Config", Order: 2)]
        [Unit("dBm")]
        public double dGainMeasSrcLevel { get; set; }

        [Display(Name: "Receiver Range for Gain Meas", Group: "VSA Config", Order: 3)]
        [Unit("dBm")]
        public double dGainMeasRcvRange { get; set; }


        [Display(Name: "RBW", Group: "VSA Config", Order: 11)]
        [Unit("MHz")]
        public double dRBW { get; set; }

        //[Display(Name: "Range", Group: "NF Cold Source", Order: 2)]
        //[Unit("dBm")]
        //public double dRange { get; set; }

        //[Display(Name: "Center Frequency", Group: "NF Cold Source", Order: 1)]
        //[Unit("MHz")]
        //public double dCenterFrequency { get; set; }

        [Display(Name: "Sweep Time", Group: "VSA Config", Order: 12)]
        [Unit("ms")]
        public double dSweepTime { get; set; }

        [Display(Name: "LoopCount", Group: "VSA Config", Order: 18)]
        public int iLoopCount { get; set; }

        [Display(Name: "Freq Mode", Group: "Measurement frequency list", Order: 21)]
        [AvailableValues("freqmodes")]
        public string freqmode
        {
            get { return _freqmode; }
            set
            {
                _freqmode = value;

                if (_freqmode.Equals("SWEPt"))
                {
                    freqMode_is_swept = true;
                    freqMode_is_list = false;
                }
                else if (_freqmode.Equals("List"))
                {
                    freqMode_is_list = true;
                    freqMode_is_swept = false;
                }
            }
        }

        [Browsable(false)]
        public bool freqMode_is_swept { get; set; }

        [Browsable(false)]
        public bool freqMode_is_list { get; set; }

        [Display(Name: "Available Values")]
        [Browsable(false)]
        public List<string> freqmodes { get; set; }

        [Display(Name: "Center Frequency", Group: "Measurement frequency list", Order: 22)]
        [Unit("MHz")]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public double dCenterFrequency { get; set; }

        [Display(Name: "Frequency Span", Group: "Measurement frequency list", Order: 23)]
        [Unit("MHz")]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public double dFrequencySpan { get; set; }

        [Display(Name: "Number of Point", Group: "Measurement frequency list", Order: 24)]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public int iNOP { get; set; }

        [Display(Name: "Measurement Frequency List", Group: "Measurement frequency list", Order: 25)]
        [EnabledIf("freqMode_is_list", true, HideIfDisabled = true)]
        public string sFrequencyList { get; set; }

        //Loss Compensation
        [Display(Name: "Loss Compensation After DUT Mode", Group: "Loss Comp", Order: 31)]
        public LossCompMode eLossCompAfterDUTMode { get; set; }

        [Display(Name: "Fixed Loss after DUT", Group: "Loss Comp", Order: 32)]
        [Unit("dB")]
        [EnabledIf("eLossCompAfterDUTMode", LossCompMode.Fixed, HideIfDisabled = true)]
        public double dFixedLossAfterDUT { get; set; }

        #region Test result
        [Display(Name: "Limit", Group: "Test Limits", Order: 41)]
        public double limit { get; set; }
        #endregion


        #endregion

        public PA_NFColdSourceMeasurement()
        {
            dGainMeasDuration = 20;
            dGainMeasSrcLevel = -30;
            dGainMeasRcvRange = -5;
            dRBW = 4.0;
            //dRange = -40;
            dSweepTime = 16;
            iLoopCount = 1;
            eLossCompAfterDUTMode = LossCompMode.Fixed;
            dFixedLossAfterDUT = 1.0;

            freqmodes = new List<string>();
            freqmodes.Add("SWEPt");
            freqmodes.Add("List");

            freqmode = "SWEPt";

            freqMode_is_swept = true;
            freqMode_is_list = false;

            dCenterFrequency = 1500;
            dFrequencySpan = 200;
            iNOP = 41;

            sFrequencyList = "1.0e9, 1.2e9, 1.4e9, 1.6e9, 1.8e9, 2.0e9";

            limit = 8.0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool pass = true;
            double time_consumed = 0;

            double[] freqList = null;

            try
            {
                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                //place measurement logic here

                NoiseFigureResult NFResult = new NoiseFigureResult();
                NoiseFigureConfigure NFConfig = new NoiseFigureConfigure();

                switch (freqmode)
                {
                    case "SWEPt":
                        NFConfig.FreqList = new double[iNOP];
                        double freqStart = dCenterFrequency * 1e6 - dFrequencySpan * 1e6 / 2;
                        double freqStep = dFrequencySpan * 1e6 / (iNOP - 1);
                        for (int i = 0; i < iNOP; i++) { NFConfig.FreqList[i] = freqStart + (i * freqStep); }
                        break;
                    case "List":
                        string[] tmpstr = sFrequencyList.Split(',');
                        NFConfig.FreqList = new double[tmpstr.Length];
                        for (int i = 0; i < tmpstr.Length; i++) { NFConfig.FreqList[i] = double.Parse(tmpstr[i].Trim()); }
                        break;
                    default:
                        break;
                }

                //
                NFConfig.Range = -28;
                NFConfig.GainMeasDuration = dGainMeasDuration * 0.001;
                NFConfig.GainMeasSrcLevel = dGainMeasSrcLevel;
                NFConfig.GainMeasRcvRange = dGainMeasRcvRange;
                NFConfig.NoiseBandWidth = dRBW * 1e6;
                NFConfig.LoopCount = iLoopCount;
                NFConfig.eLossCompAfterDUT = eLossCompAfterDUTMode;
                NFConfig.dFixedLossAfterDUT = dFixedLossAfterDUT;
                NFConfig.SweepTime = dSweepTime;
                NFConfig.CalFileName = Utility.getNSCalFileNameForPath();

                PlugInKMF plugInKmf = GetInstrument().pluginKmf;

                plugInKmf.MeasureNoiseFigureColdSource(NFConfig, ref NFResult);

                sw.Stop();

                bool LogNFResult = true;

                if (LogNFResult)
                {
                    string dummy = "Freq(Hz), NF mean(dB), NF Repeatability(dBrms), NF Repeatability(dBpp), Gain(dB),";
                    for (int i = 0; i < NFConfig.LoopCount; i++)
                    {
                        dummy += $", Count{(i + 1)}";
                    }
                    Logging(LogLevel.INFO, dummy);

                    for (int i = 0; i < NFResult.frequency.Length; i++)
                    {
                        dummy = $"{NFResult.frequency[i]}, {NFResult.MeasAvg[i]}, {NFResult.MeasRms[i]}, {NFResult.MeasPP[i]}, {NFResult.Gain[0][i]}, ";
                        for (int j = 0; j < NFConfig.LoopCount; j++)
                        {
                            dummy += $", {NFResult.NFdata[j][i]}";
                        }
                        Logging(LogLevel.INFO, dummy);
                    }
                }


                for (int i = 0; i < NFResult.frequency.Length; i++)
                {
                    for (int j = 0; j < NFConfig.LoopCount; j++)
                    {
                        if (NFResult.NFdata[j][i] > limit || double.IsNaN(NFResult.NFdata[j][i]) == true)
                        {
                            pass = false;
                            Logging(LogLevel.INFO, "The NF result of frequency-{0}-Count[{1}]-failed, test result is {2}",
                                   NFResult.frequency[i], j, NFResult.NFdata[j][i]);
                        }
                    }

                    //Only publish the first value measured at the specific frequency instead of all the values in the loop.
                    Results.Publish("Cold Source NF", new List<string> { "Technology", "Frequency(MHz)", "LimitLow", "LimitHigh", "Value", "Unit", "Passed", "TestTime" },
                                                                         GetInstrument().selected_technology,
                                                                         NFResult.frequency[i] / 1e6,
                                                                         "-999",
                                                                         limit,
                                                                         NFResult.NFdata[0][i],
                                                                         "",
                                                                         pass,
                                                                         0
                                                                         );

                }

                Results.Publish("Cold Source NF", new List<string> { "pass" }, pass);

                //Logging(LogLevel.INFO, "MeasureNoiseFigureColdSource return {0}:", pass);

                PA_LOG(PA_LOG_TRS, "NFCS ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Noise Figure(Code Source) Done,Time:{3,7:F2}ms",
                      pass ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "MeasureNoiseFigureColdSource return {0}:", pass);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }

    }

    [Display(Name: "Noise Figure Y-Factor", Groups: new string[] { "PA", "Measurements" })]
    [Description("Noise Figure Y-Factor")]
    public class PA_NFYFactorMeasurement : PA_TestStep
    {
        private string _freqmode;
        #region Settings

        [Display(Name: "RBW", Group: "VSA Config", Order: 11)]
        [Unit("MHz")]
        public double dRBW { get; set; }

        //[Display(Name: "Range", Group: "NF Cold Source", Order: 2)]
        //[Unit("dBm")]
        //public double dRange { get; set; }

        //[Display(Name: "Center Frequency", Group: "NF Cold Source", Order: 1)]
        //[Unit("MHz")]
        //public double dCenterFrequency { get; set; }

        [Display(Name: "Sweep Time", Group: "VSA Config", Order: 12)]
        [Unit("ms")]
        public double dSweepTime { get; set; }

        [Display(Name: "LoopCount", Group: "VSA Config", Order: 18)]
        public int iLoopCount { get; set; }

        [Display(Name: "Freq Mode", Group: "Measurement frequency list", Order: 21)]
        [AvailableValues("freqmodes")]
        public string freqmode
        {
            get { return _freqmode; }
            set
            {
                _freqmode = value;

                if (_freqmode.Equals("SWEPt"))
                {
                    freqMode_is_swept = true;
                    freqMode_is_list = false;
                }
                else if (_freqmode.Equals("List"))
                {
                    freqMode_is_list = true;
                    freqMode_is_swept = false;
                }
            }
        }

        [Browsable(false)]
        public bool freqMode_is_swept { get; set; }

        [Browsable(false)]
        public bool freqMode_is_list { get; set; }

        [Display(Name: "Available Values")]
        [Browsable(false)]
        public List<string> freqmodes { get; set; }

        [Display(Name: "Center Frequency", Group: "Measurement frequency list", Order: 22)]
        [Unit("MHz")]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public double dCenterFrequency { get; set; }

        [Display(Name: "Frequency Span", Group: "Measurement frequency list", Order: 23)]
        [Unit("MHz")]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public double dFrequencySpan { get; set; }

        [Display(Name: "Number of Point", Group: "Measurement frequency list", Order: 24)]
        [EnabledIf("freqMode_is_swept", true, HideIfDisabled = true)]
        public int iNOP { get; set; }

        [Display(Name: "Measurement Frequency List", Group: "Measurement frequency list", Order: 25)]
        [EnabledIf("freqMode_is_list", true, HideIfDisabled = true)]
        public string sFrequencyList { get; set; }

        //Loss Compensation
        [Display(Name: "Loss Compensation Before DUT Mode", Group: "Loss Comp", Order: 31)]
        public LossCompMode eLossCompBeforeDUTMode { get; set; }

        [Display(Name: "Fixed Loss before DUT", Group: "Loss Comp", Order: 32)]
        [Unit("dB")]
        [EnabledIf("eLossCompBeforeDUTMode", LossCompMode.Fixed, HideIfDisabled = true)]
        public double dFixedLossBeforeDUT { get; set; }

        [Display(Name: "Loss Compensation After DUT Mode", Group: "Loss Comp", Order: 33)]
        public LossCompMode eLossCompAfterDUTMode { get; set; }

        [Display(Name: "Fixed Loss after DUT", Group: "Loss Comp", Order: 34)]
        [Unit("dB")]
        [EnabledIf("eLossCompAfterDUTMode", LossCompMode.Fixed, HideIfDisabled = true)]
        public double dFixedLossAfterDUT { get; set; }

        #region Test result
        [Display(Name: "Limit", Group: "Test Limits", Order: 41)]
        public double limit { get; set; }
        #endregion

        #endregion

        public PA_NFYFactorMeasurement()
        {
            dRBW = 4.0;
            //dRange = -40;
            dSweepTime = 16;
            iLoopCount = 2;
            eLossCompBeforeDUTMode = LossCompMode.SysCal;
            eLossCompAfterDUTMode = LossCompMode.SysCal;

            dFixedLossBeforeDUT = 1.0;
            dFixedLossAfterDUT = 1.0;

            freqmodes = new List<string>();
            freqmodes.Add("SWEPt");
            freqmodes.Add("List");

            freqmode = "SWEPt";

            freqMode_is_swept = true;
            freqMode_is_list = false;

            dCenterFrequency = 1500;
            dFrequencySpan = 200;
            iNOP = 41;

            sFrequencyList = "1.0e9, 1.2e9, 1.4e9, 1.6e9, 1.8e9, 2.0e9";

            limit = 8.0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool pass = true;
            double time_consumed = 0;

            double[] freqList = null;

            try
            {
                // ToDo: Add test case code here
                PlugInMeas meas = GetInstrument().Measurement;

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                //place measurement logic here

                NoiseFigureResult NFResult = new NoiseFigureResult();
                NoiseFigureConfigure NFConfig = new NoiseFigureConfigure();

                switch (freqmode)
                {
                    case "SWEPt":
                        NFConfig.FreqList = new double[iNOP];
                        double freqStart = dCenterFrequency * 1e6 - dFrequencySpan * 1e6 / 2;
                        double freqStep = dFrequencySpan * 1e6 / (iNOP - 1);
                        for (int i = 0; i < iNOP; i++) { NFConfig.FreqList[i] = freqStart + (i * freqStep); }
                        break;
                    case "List":
                        string[] tmpstr = sFrequencyList.Split(',');
                        NFConfig.FreqList = new double[tmpstr.Length];
                        for (int i = 0; i < tmpstr.Length; i++) { NFConfig.FreqList[i] = double.Parse(tmpstr[i].Trim()); }
                        break;
                    default:
                        break;
                }

                //
                NFConfig.Range = -28;
                NFConfig.NoiseBandWidth = dRBW * 1e6;
                NFConfig.LoopCount = iLoopCount;

                NFConfig.eLossCompBeforeDUT = eLossCompBeforeDUTMode;
                NFConfig.dFixedLossBeforeDUT = dFixedLossBeforeDUT;
                NFConfig.eLossCompAfterDUT = eLossCompAfterDUTMode;
                NFConfig.dFixedLossAfterDUT = dFixedLossAfterDUT;

                NFConfig.SweepTime = dSweepTime;
                NFConfig.CalFileName = Utility.getNSCalFileNameForPath();

                PlugInKMF plugInKmf = GetInstrument().pluginKmf;

                plugInKmf.MeasureNoiseFigureYFactor(NFConfig, ref NFResult);

                sw.Stop();

                bool LogNFResult = true;

                if (LogNFResult)
                {
                    string dummy = "Freq(Hz), NF mean(dB), NF Repeatability(dBrms), NF Repeatability(dBpp), Gain(dB),";
                    for (int i = 0; i < NFConfig.LoopCount; i++)
                    {
                        dummy += $", Count{(i + 1)}";
                    }
                    Logging(LogLevel.INFO, dummy);

                    for (int i = 0; i < NFResult.frequency.Length; i++)
                    {
                        dummy = $"{NFResult.frequency[i]}, {NFResult.MeasAvg[i]}, {NFResult.MeasRms[i]}, {NFResult.MeasPP[i]}, {NFResult.Gain[0][i]}, ";
                        for (int j = 0; j < NFConfig.LoopCount; j++)
                        {
                            dummy += $", {NFResult.NFdata[j][i]}";
                        }
                        Logging(LogLevel.INFO, dummy);
                    }
                }

                // remove count item, add limit high, limit low-null, pass/fail, test time()
                for (int i = 0; i < NFResult.frequency.Length; i++)
                {
                    for (int j = 0; j < NFConfig.LoopCount; j++)
                    {
                        if (NFResult.NFdata[j][i] > limit)
                        {
                            pass = false;
                            Logging(LogLevel.INFO, "The NF result of frequency-{0}-Count[{1}]-failed, test result is {2}",
                                   NFResult.frequency[i], j, NFResult.NFdata[j][i]);
                        }
                    }

                    //Only publish the first value measured at the specific frequency instead of all the values in the loop.
                    Results.Publish("Y-Factor NF", new List<string> { "Technology", "Frequency(MHz)", "LimitLow", "LimitHigh", "Value", "Unit", "Passed", "TestTime" },
                                                                         GetInstrument().selected_technology,
                                                                         NFResult.frequency[i] / 1e6,
                                                                         "-999",
                                                                         limit,
                                                                         NFResult.NFdata[0][i],
                                                                         "",
                                                                         pass,
                                                                         0
                                                                         );

                }

                //Results.Publish("Y-Factor NF", new List<string> { "pass" }, pass);

                PA_LOG(PA_LOG_TRS, "NFYC ", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Noise Figure(Y-Factor) Done,Time:{3,7:F2}ms",
                      pass ? "Pass" : "Fail",
                      TechnologyName(GetInstrument().selected_technology),
                      GetInstrument().selected_frequency,
                      (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error("Exception {0} !", ex.ToString());
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }
    #endregion

    public abstract class PA_KmfTestStep : PA_TestStep
    {
        [Display(Name: "KMF Troubleshooting", Order: 1)]
        [Browsable(true)]
        public bool enable_kmf_debug { set; get; }

        [Display(Name: "KMF Result Viewer", Order: 2)]
        //[EnabledIf("meas_type", measurementType.KMF, HideIfDisabled = true)]
        [Browsable(true)]
        public void Import()
        {
            // How to dispose the previous one when closing?
            if (GetInstrument().kmfResultWindow != null)
            {
                GetInstrument().kmfResultWindow = null;
            }

            try
            {
                GetInstrument().kmfResultWindow = new KmfResultWindow();
                GetInstrument().kmfResultWindow.Show();
            }
            catch (Exception ex)
            {

            }
        }
        public PA_KmfTestStep()
        {
            enable_kmf_debug = false;
        }
    }

    [Display(Name: "EVM", Groups: new string[] { "PA", "Measurements" })]
    public class PA_EvmMeasurementKmf : PA_KmfTestStep
    {
        #region Settings
        [Display(Name: "Add Noise Reduction", Group: "Noise Reduction", Order: 10)]
        [Browsable(true)]
        public bool NoiseReductionUsedinEVM { get; set; }

        [Display(Name: "Set Receiver Range", Group: "EVM Setup", Order: 10.1)]
        [Browsable(true)]
        public bool set_range { get; set; }

        [Display(Name: "Receiver Range", Group: "EVM Setup", Order: 10.2)]
        [Unit("dBm")]
        [Browsable(true)]
        [EnabledIf("set_range", true, HideIfDisabled = true)]
        public double rcv_range { get; set; }

        [Display(Name: "Search Length", Group: "EVM Setup", Order: 10.3)]
        [Unit("s")]
        [Browsable(true)]
        public double search_length { get; set; }


        [Display(Name: "Low Limit (Cellular)", Group: "Test Limits(Cellular)", Order: 11)]
        [Unit("%")]
        [Browsable(true)]
        public double LowLimit_Cell { get; set; }

        [Display(Name: "High Limit (Cellular)", Group: "Test Limits(Cellular)", Order: 12)]
        [Unit("%")]
        [Browsable(true)]
        public double HighLimit_Cell { get; set; }

        [Display(Name: "Low Limit (WLAN)", Group: "Test Limits(WLAN)", Order: 21)]
        [Unit("dB")]
        [Browsable(true)]
        public double LowLimit_WLAN { get; set; }

        [Display(Name: "High Limit (WLAN)", Group: "Test Limits(WLAN)", Order: 22)]
        [Unit("dB")]
        [Browsable(true)]
        public double HighLimit_WLAN { get; set; }

        [Display(Name: "Use HD", Group: "Test", Order: 30)]
        [Browsable(false)]
        public bool use_hd { get; set; }

        [Display(Name: "range", Group: "Test", Order: 30)]
        [Browsable(false)]
        [Unit("dBm")]
        [EnabledIf("use_hd", true, HideIfDisabled = true)]
        public double power_range { get; set; }
        #endregion


        public double[] evmData;
        public KmfEvmResultView evmResult = new KmfEvmResultView();

        // For debug purpose;
        // Create the GUI and KMF display controls

        private KmfResultWindow _KmfResultWindow;
        public event UpdateKmfResultEventHandle KmfResultUpdate;

        public PA_EvmMeasurementKmf()
        {
            NoiseReductionUsedinEVM = false;
            LowLimit_Cell = 0.0;
            HighLimit_Cell = 5.0;

            LowLimit_WLAN = -998;
            HighLimit_WLAN = -40;

            use_hd = false;
            power_range = 30;
            search_length = 1e-3;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            evmResult.Update(-999, -999, -999, -999, -999);

            _KmfResultWindow = GetInstrument().kmfResultWindow;

            if (_KmfResultWindow != null && _KmfResultWindow.IsVisible == true)
            {
                try
                {
                    _KmfResultWindow.Subscribe(this);
                    //GetInstrument().pluginKmf.PrepareDisplay(_KmfResultWindow.pgEvmResult._graphical_waveForm, _KmfResultWindow.pgEvmResult._graphical_measure, TESTTYPE.EVM, 122880);
                }
                catch (Exception ex)
                {
                    Logging(LogLevel.ERROR, "KMF EVM Failure: " + ex.Message);
                }
            }
            else if (_KmfResultWindow != null)
            {
                try
                {
                    GetInstrument().pluginKmf.DisposeDisplay();
                    _KmfResultWindow.Dispose(this);
                }
                catch (Exception ex)
                {
                    Logging(LogLevel.ERROR, "KMF EVM Failure: " + ex.Message);
                }
            }
        }

        public override void Run()
        {
            bool pass = true;
            double HighLimit;
            double LowLimit;
            try
            {
                GetInstrument().pluginKmf.enable_debug = enable_kmf_debug;

                if (_KmfResultWindow != null && _KmfResultWindow.IsVisible == true)
                {
                    GetInstrument().pluginKmf.PrepareEvmDisplay(_KmfResultWindow.pgEvmResult._graphical_waveForm, _KmfResultWindow.pgEvmResult._graphical_measure, 122880);
                }

                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                switch (ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology))
                {
                    case SUPPORTEDFORMAT.WLAN11N:
                    case SUPPORTEDFORMAT.WLAN11AC:
                    case SUPPORTEDFORMAT.WLAN11AX:
                    case SUPPORTEDFORMAT.WLAN11A:
                    case SUPPORTEDFORMAT.WLAN11G:
                    case SUPPORTEDFORMAT.WLAN11J:
                    case SUPPORTEDFORMAT.WLAN11P:
                        HighLimit = HighLimit_WLAN;
                        LowLimit = LowLimit_WLAN;
                        break;
                    default:
                        HighLimit = HighLimit_Cell;
                        LowLimit = LowLimit_Cell;
                        break;
                }

                MeasureOrNot(true, HighLimit, LowLimit, TestItem_Enum.EVM, tps);
                GetInstrument().pluginKmf.evmErrorMsg = string.Empty;
                if (set_range && GetInstrument().vsa != null)
                {
                    Logging(LogLevel.INFO, "Receiver range adjust from {0}dBm to {1}dBm", GetInstrument().vsa.Power, rcv_range);
                    GetInstrument().vsa.Power = rcv_range;
                    GetInstrument().vsa.Apply();
                }

                if (ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.LTEAFDD ||
                    ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.LTEATDD)
                {
                    pass = ps.KmfMeasureLTEAEVM(GetInstrument().selected_technology,
                                            GetInstrument().selected_frequency * 1e6,
                                            search_length,
                                            tps);

                    /* Publish SXQ_TMP*/
                    PublishResult_LTEAEvm(true, GetInstrument().selected_technology, GetInstrument().selected_frequency, HighLimit, LowLimit, TestItem_Enum.EVM, tps, Results);
                }
                else
                {
                    if (use_hd)
                        pass = ps.KmfMeasureEVM_HD(power_range, GetInstrument().selected_technology,
                                            GetInstrument().selected_frequency * 1e6,
                                            search_length,
                                            tps);
                    else
                        pass = ps.KmfMeasureEVM(NoiseReductionUsedinEVM, GetInstrument().selected_technology,
                                                GetInstrument().selected_frequency * 1e6,
                                                search_length,
                                                tps);

                    PublishResult1(true, GetInstrument().selected_technology, GetInstrument().selected_frequency, HighLimit, LowLimit, TestItem_Enum.EVM, tps, Results);
                }

                /* Update Result Reviewer */
                UpdateKmfResultEventArgs e = new UpdateKmfResultEventArgs();
                e.MeasType = KMFMEASTYPE.KMF_EVM;
                e.evmResult = new EVMResult();

                switch (ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology))
                {
                    case SUPPORTEDFORMAT.LTEAFDD:
                    case SUPPORTEDFORMAT.LTEATDD:
                        e.evmResult.isLTEA = true;
                        e.evmResult.CarrierEvm = new double[tps[TestItem_Enum.EVM].ComponentCarrierEvm.Count()];
                        for (int i = 0; i < tps[TestItem_Enum.EVM].ComponentCarrierEvm.Count(); i++)
                        {
                            e.evmResult.CarrierEvm[i] = tps[TestItem_Enum.EVM].ComponentCarrierEvm[i];
                        }
                        break;

                    case SUPPORTEDFORMAT.WLAN11N:
                    case SUPPORTEDFORMAT.WLAN11AC:
                    case SUPPORTEDFORMAT.WLAN11AX:
                    case SUPPORTEDFORMAT.WLAN11A:
                    case SUPPORTEDFORMAT.WLAN11G:
                    case SUPPORTEDFORMAT.WLAN11J:
                    case SUPPORTEDFORMAT.WLAN11P:
                        e.evmResult.isWlan = true;
                        e.evmResult.Evm = tps[TestItem_Enum.EVM].TestResult;
                        break;

                    default:
                        e.evmResult.isWlan = false;
                        e.evmResult.Evm = tps[TestItem_Enum.EVM].TestResult;
                        break;
                }

                if (KmfResultUpdate != null)
                    KmfResultUpdate(this, e);


                RunChildSteps(); //If step has child steps.

                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "KMF EVM Failure: " + GetInstrument().pluginKmf.evmErrorMsg);
                }

                //Logging(LogLevel.INFO, "time_acquisition = {0}, time_calevm = {1}, time_total = {2}, tech = {3}",
                //      ps.time_IQAc, ps.time_cal, ps.time_total, GetInstrument().selected_technology );
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
                Logging(LogLevel.ERROR, "KMF EVM Failure: " + ex.Message);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();

        }
    }

    [Display(Name: "EVM Calculation", Groups: new string[] { "PA", "Measurements" })]
    public class PA_EvmCalculationKmf : PA_TestStep
    {
        #region Settings
        [Display(Name: "Add Noise Reduction", Group: "Noise Reduction", Order: 10)]
        [Browsable(true)]
        public bool NoiseReductionUsedinEVM { get; set; }

        [Display(Name: "Search Length", Group: "EVM Setup", Order: 10.3)]
        [Unit("s")]
        [Browsable(true)]
        public double search_length { get; set; }

        [Display(Name: "Low Limit (Cellular)", Group: "Test Limits(Cellular)", Order: 11)]
        [Unit("%")]
        [Browsable(true)]
        public double LowLimit_Cell { get; set; }

        [Display(Name: "High Limit (Cellular)", Group: "Test Limits(Cellular)", Order: 12)]
        [Unit("%")]
        [Browsable(true)]
        public double HighLimit_Cell { get; set; }

        [Display(Name: "Low Limit (WLAN)", Group: "Test Limits(WLAN)", Order: 21)]
        [Unit("dB")]
        [Browsable(true)]
        public double LowLimit_WLAN { get; set; }

        [Display(Name: "High Limit (WLAN)", Group: "Test Limits(WLAN)", Order: 22)]
        [Unit("dB")]
        [Browsable(true)]
        public double HighLimit_WLAN { get; set; }

        [Display(Name: "Use HD", Group: "Test", Order: 30)]
        [Browsable(false)]
        public bool use_hd { get; set; }

        [Display(Name: "range", Group: "Test", Order: 30)]
        [Browsable(false)]
        [Unit("dBm")]
        [EnabledIf("use_hd", true, HideIfDisabled = true)]
        public double power_range { get; set; }
        #endregion

        public double[] evmData;

        public PA_EvmCalculationKmf()
        {
            NoiseReductionUsedinEVM = false;
            LowLimit_Cell = 0.0;
            HighLimit_Cell = 5.0;

            LowLimit_WLAN = -998;
            HighLimit_WLAN = -40;

            use_hd = false;
            power_range = 30;
            search_length = 1e-3;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }
        private void SetLimitation(Dictionary<TestItem_Enum, TestPoint> t, out double HighLimit, out double LowLimit)
        {
            HighLimit = 0.0;
            LowLimit = 0.0;

            switch (GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology))
            {
                case SUPPORTEDFORMAT.WLAN11N:
                case SUPPORTEDFORMAT.WLAN11AC:
                case SUPPORTEDFORMAT.WLAN11AX:
                case SUPPORTEDFORMAT.WLAN11A:
                case SUPPORTEDFORMAT.WLAN11G:
                case SUPPORTEDFORMAT.WLAN11J:
                case SUPPORTEDFORMAT.WLAN11P:
                    HighLimit = HighLimit_WLAN;
                    LowLimit = LowLimit_WLAN;
                    break;
                default:
                    HighLimit = HighLimit_Cell;
                    LowLimit = LowLimit_Cell;
                    break;
            }

            MeasureOrNot(true, HighLimit, LowLimit, TestItem_Enum.EVM, t);
        }
        
        public override void Run()
        {
            bool pass = true;
            double HighLimit = 0.0;
            double LowLimit = 0.0;
            try
            {
                PlugInMeas ps = GetInstrument().Measurement;
                IVSA vsa = GetInstrument().vsa;

                if (vsa.IQInfo == null || vsa.IQInfo.IQ == null)
                {
                    Logging(LogLevel.ERROR, "KMF EVM Calculation Failure: No IQ data available");
                    return;
                }
                IQDataInfo iq_info;
                lock (vsa.IQInfo)
                    iq_info = (IQDataInfo)vsa.IQInfo.Clone();

                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                SetLimitation(tps, out HighLimit, out LowLimit);

                GetInstrument().pluginKmf.evmErrorMsg = string.Empty;

                if (ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.LTEAFDD ||
                    ps.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.LTEATDD)
                {
                    pass = ps.KmfMeasureLTEAEVM(iq_info.tech,
                                            iq_info.frequency,
                                            search_length,
                                            tps, 
                                            false, SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
                                            false, iq_info.IQ);

                    /* Publish SXQ_TMP*/
                    PublishResult_LTEAEvm(true, iq_info.tech, iq_info.frequency / 1e6, HighLimit, LowLimit, TestItem_Enum.EVM, tps, Results);
                }
                else
                {
                    if (use_hd)
                        Logging(LogLevel.ERROR, "KMF EVM Calculation Failure: No Hardware should be involved");
                    else
                        pass = ps.KmfMeasureEVM(NoiseReductionUsedinEVM, iq_info.tech,
                                                iq_info.frequency,
                                                search_length,
                                                tps,
                                                false, SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
                                                false, iq_info.IQ);

                    PublishResult1(true, iq_info.tech, iq_info.frequency / 1e6, HighLimit, LowLimit, TestItem_Enum.EVM, tps, Results);
                }

                RunChildSteps(); //If step has child steps.

                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "KMF EVM Calculation Failure: " + GetInstrument().pluginKmf.evmErrorMsg);
                }

                //Logging(LogLevel.INFO, "time_acquisition = {0}, time_calevm = {1}, time_total = {2}, tech = {3}",
                //      ps.time_IQAc, ps.time_cal, ps.time_total, GetInstrument().selected_technology );
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
                Logging(LogLevel.ERROR, "KMF EVM Calculation Failure: " + ex.Message);
            }
        }
        public override void PostPlanRun()
        {
            base.PostPlanRun();

        }
    }


    [Display(Name: "ORFS", Groups: new string[] { "PA", "Measurements" })]
    public class PA_ORFSMeasurememtKmf : PA_KmfTestStep
    {
        #region Setting
        [Display(Name: "Average", Group: "Measure Setup", Order: 1)]
        [Browsable(true)]
        public bool averageEnable { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 2)]
        [Browsable(true)]
        [EnabledIf("averageEnable", true, HideIfDisabled = true)]
        public int numOfAverage { get; set; }

        [Display(Name: "Trigger Source", Group: "Measure Setup", Order: 3)]
        [Browsable(true)]
        public ORFSTRIGGERMODE triggersource { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measure Setup", Order: 4)]
        [Browsable(true)]
        [Unit("s")]
        [EnabledIf("triggersource", ORFSTRIGGERMODE.External, ORFSTRIGGERMODE.RFBurst, HideIfDisabled = true)]
        public double triggerdelay { get; set; }

        [Display(Name: "Trigger Level", Group: "Measure Setup", Order: 5)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("triggersource", ORFSTRIGGERMODE.RFBurst, HideIfDisabled = true)]
        public double triggerlevel_dbm { get; set; }

        [Display(Name: "Trigger Level", Group: "Measure Setup", Order: 6)]
        [Browsable(true)]
        [Unit("volt")]
        [EnabledIf("triggersource", ORFSTRIGGERMODE.External, HideIfDisabled = true)]
        public double triggerlevel_v { get; set; }

        [Display(Name: "Type", Group: "ORFS Setup", Order: 12)]
        [Browsable(true)]
        public KMFORFSType type { get; set; }

        [Display(Name: "Modulation Average", Group: "ORFS Setup", Order: 13)]
        [EnabledIf("type", KMFORFSType.Modulation, KMFORFSType.ModulationAndSwitching, HideIfDisabled = true)]
        public AverageType averagetype { get; set; }

        [Display(Name: "Burst Type", Group: "ORFS Setup", Order: 16)]
        [Browsable(true)]
        public KMFORFSBurst burst { get; set; }

        [Display(Name: "Burst Interval", Group: "ORFS Setup", Order: 17)]
        [Browsable(true)]
        [Unit("s")]
        public double burstInterval { get; set; }

        [Display(Name: "Burst Start Offset", Group: "ORFS Setup", Order: 18)]
        [Browsable(true)]
        [Unit("s")]
        public double burstStartOffset { get; set; }

        [Display(Name: "Number of Bursts", Group: "ORFS Setup", Order: 19)]
        [Browsable(true)]
        public int numOfBursts { get; set; }

        [Display(Name: "Manual Power Range", Group: "ORFS Setup", Order: 20)]
        [Browsable(true)]
        public bool manual_range { get; set; }

        [Display(Name: "Power Range", Group: "ORFS Setup", Order: 21)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("manual_range", true, HideIfDisabled = true)]
        public double range { get; set; }

        [Display(Name: "Limit Config", Group: "Offset & Limit", Order: 32)]
        [Browsable(true)]
        public KMFLimitConfig offsetLimit { get; set; }

        [Display(Name: "Band", Group: "Offset & Limit", Order: 33)]
        [Browsable(true)]
        [EnabledIf("offsetLimit", KMFLimitConfig.Preset, HideIfDisabled = true)]
        public GSMBand band { get; set; }

        [Display(Name: "Use 8PSK", Group: "Offset & Limit", Order: 34)]
        [Browsable(true)]
        [EnabledIf("offsetLimit", KMFLimitConfig.Preset, HideIfDisabled = true)]
        public bool is8psk { get; set; }

        private string _config_file;
        [Display(Name: "Config File", Group: "Offset & Limit", Order: 35)]
        [EnabledIf("offsetLimit", KMFLimitConfig.Custom, HideIfDisabled = true)]
        [FilePath]
        public string config_file
        {
            get { return _config_file; }
            set
            {
                _config_file = value;
            }
        }

        //[Display(Name: "PCL Configure", Group: "Power Level", Order: 40)]
        //[Browsable(true)]
        //public KMFORFSPCL pclConfig { get; set; }

        //[Display(Name: "PCL", Group: "Power Level", Order: 41)]
        //[Browsable(true)]
        //[EnabledIf("pclConfig", KMFORFSPCL.Manual, HideIfDisabled = true)]
        //public int pcl { get; set; }
        #endregion

        private ORFSResult OrfsResult = null;
        private KmfResultWindow _KmfResultWindow;
        public event UpdateKmfResultEventHandle KmfResultUpdate;

        private OrfsUserConfigure user_setting = new OrfsUserConfigure();

        public PA_ORFSMeasurememtKmf()
        {
            averageEnable = false;
            numOfAverage = 10;
            triggersource = ORFSTRIGGERMODE.External;
            manual_range = false;
            range = 0;
            numOfBursts = 1;

            offsetLimit = KMFLimitConfig.Preset;
            band = GSMBand.GSM;
            is8psk = false;

            burstInterval = 576.92e-6 * 8;
            burstStartOffset = 200e-6;
            triggerdelay = -200e-6;
            triggerlevel_v = 1.5;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();

            _KmfResultWindow = GetInstrument().kmfResultWindow;

            if ((_KmfResultWindow != null) && (_KmfResultWindow.IsVisible == true))
            {
                _KmfResultWindow.Subscribe(this);
            }
        }
        public override void Run()
        {
            bool testResult = true;
            OrfsResult = new ORFSResult();
            try
            {
                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                GetInstrument().pluginKmf.enable_debug = enable_kmf_debug;

                CollectUserSetting();

                MeasureOrNot(true, 999, -999, TestItem_Enum.ORFS, tps);




                testResult = ps.KmfMeasureORFS(user_setting,
                                               tps,
                                               ref OrfsResult);

                if (!testResult)
                    OrfsResult.result = "Fail";

                Logging(LogLevel.INFO, "KMF orfs go cost {0} ms\n", GetInstrument().pluginKmf.orfs_time);

                UpdateKmfResultEventArgs e = new UpdateKmfResultEventArgs();
                e.MeasType = KMFMEASTYPE.KMF_ORFS;
                e.orfsResult = OrfsResult;

                if (KmfResultUpdate != null)
                    KmfResultUpdate(this, e);

                PublishResult_Orfs(true, GetInstrument().selected_technology, GetInstrument().selected_frequency,
                                   OrfsResult, Results);

                RunChildSteps(); //If step has child steps.

                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (OrfsResult.result == "Pass")
                    UpgradeVerdict(Verdict.Pass);
                else
                    UpgradeVerdict(Verdict.Fail);
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }

        private void CollectUserSetting()
        {
            user_setting.centreFrequency = GetInstrument().selected_frequency * 1e6;
            user_setting.meastype = type;
            user_setting.averagetype = averagetype;
            user_setting.manual_range = manual_range;
            user_setting.range = range;
            user_setting.bursttype = burst;
            user_setting.numofBurst = numOfBursts;
            user_setting.triggersrc = triggersource;
            user_setting.triggerdelay = triggerdelay;
            user_setting.triggerlevel = (triggersource == ORFSTRIGGERMODE.External) ? triggerlevel_v : triggerlevel_dbm;

            user_setting.averageEnable = averageEnable;
            user_setting.numOfAverage = numOfAverage;

            user_setting.burstStartOffset = burstStartOffset;
            user_setting.burstInterval = burstInterval;

            user_setting.limitConfig = offsetLimit;
            user_setting.configFile = config_file;
            user_setting.is8psk = is8psk;
            user_setting.band = band;
        }
    }

    [Display(Name: "SEM", Groups: new string[] { "PA", "Measurements" })]
    public class PA_SEMMeasurementKmf : PA_KmfTestStep
    {
        private const string M938xKmfPluginName = "Keysight.KMF.Resources.M938x.IviComDriver";
        private const string M9391KmfPluginName = "Keysight.Kmf.Resources.M9391.IviComDriver";
        private const string M9420KmfPluginName = "Keysight.Kmf.Resources.M9420.IviComDriver";

        #region Setting
        [Display(Name: "Average", Group: "Measure Setup", Order: 1)]
        [Browsable(true)]
        public bool averageEnable { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 2)]
        [Browsable(true)]
        [EnabledIf("averageEnable", true, HideIfDisabled = true)]
        public int numOfAverage { get; set; }

        [Display(Name: "Trigger Source", Group: "Measure Setup", Order: 3)]
        [Browsable(true)]
        public SEMTRIGGERMODE triggersource { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measure Setup", Order: 4)]
        [Browsable(true)]
        [Unit("s")]
        [EnabledIf("triggersource", SEMTRIGGERMODE.External, SEMTRIGGERMODE.RFBurst, HideIfDisabled = true)]
        public double triggerdelay { get; set; }

        [Display(Name: "Trigger Level", Group: "Measure Setup", Order: 5)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("triggersource", SEMTRIGGERMODE.RFBurst, HideIfDisabled = true)]
        public double triggerlevel_dbm { get; set; }

        [Display(Name: "Trigger Level", Group: "Measure Setup", Order: 6)]
        [Browsable(true)]
        [Unit("volt")]
        [EnabledIf("triggersource", SEMTRIGGERMODE.External, HideIfDisabled = true)]
        public double triggerlevel_v { get; set; }

        [Display(Name: "Average Type", Group: "SEM", Order: 11)]
        [Browsable(true)]
        public AverageType average_type { get; set; }

        [Display(Name: "Manual Power Range", Group: "SEM", Order: 12)]
        [Browsable(true)]
        public bool manual_range { get; set; }

        [Display(Name: "Power Range", Group: "SEM", Order: 13)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("manual_range", true, HideIfDisabled = true)]
        public double range { get; set; }

        [Display(Name: "Points", Group: "Test", Order: 31)]
        [Browsable(false)]
        public int points { get; set; }

        [Display(Name: "Config", Group: "Offset & Limit", Order: 50)]
        [Browsable(true)]
        public KMFLimitConfig offsetLimit { get; set; }

        private string _config_file;
        [Display(Name: "Config File", Group: "Offset & Limit", Order: 51)]
        [EnabledIf("offsetLimit", KMFLimitConfig.Custom, HideIfDisabled = true)]
        [FilePath]
        public string config_file
        {
            get { return _config_file; }
            set
            {
                _config_file = value;
            }
        }

        #endregion
        private PlugInKMF plugInKmf = null;

        public bool testResult = true;
        public KmfSemResultView semResultView = new KmfSemResultView();
        public SEMResult semResult = null;

        private SemUserConfigure user_setting = new SemUserConfigure();

        private KmfResultWindow _KmfResultWindow;
        public event UpdateKmfResultEventHandle KmfResultUpdate;

        public PA_SEMMeasurementKmf()
        {
            averageEnable = false;
            numOfAverage = 10;
            average_type = AverageType.Log;

            triggersource = SEMTRIGGERMODE.Immediate;
            triggerlevel_v = 2.0;
            triggerlevel_dbm = -20;

            points = 8192;
            offsetLimit = KMFLimitConfig.Preset;

            manual_range = false;
            range = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();

            plugInKmf = GetInstrument().pluginKmf;

            _KmfResultWindow = GetInstrument().kmfResultWindow;

            if (_KmfResultWindow != null && _KmfResultWindow.IsVisible == true)
            {
                try
                {
                    _KmfResultWindow.Subscribe(this);
                    //GetInstrument().pluginKmf.PrepareSEMDisplay(_KmfResultWindow.pgSemResult._graphical_waveform,
                    //                                          _KmfResultWindow.pgSemResult._graphical_sem);
                }
                catch (Exception ex)
                {
                    Logging(LogLevel.ERROR, "KMF EVM Failure: " + ex.Message);
                }
            }
            else if (_KmfResultWindow != null)
            {
                try
                {
                    GetInstrument().pluginKmf.DisposeDisplay();
                    _KmfResultWindow.Dispose(this);
                }
                catch (Exception ex)
                {
                    Logging(LogLevel.ERROR, "KMF EVM Failure: " + ex.Message);
                }
            }
        }

        public override void Run()
        {
            bool pass = true;
            double HighLimit;
            double LowLimit;
            semResult = new SEMResult();
            try
            {
                GetInstrument().pluginKmf.enable_debug = enable_kmf_debug;
                if (_KmfResultWindow != null && _KmfResultWindow.IsVisible == true)
                {
                    GetInstrument().pluginKmf.PrepareSEMDisplay(_KmfResultWindow.pgSemResult._graphical_waveform,
                                                              _KmfResultWindow.pgSemResult._graphical_sem);
                }

                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(true, 999, -999, TestItem_Enum.SEM, tps);
                DIRECTION dir = DIRECTION.UPLINK;

                ps.TestConditionSetup_GetWaveformInfo(GetInstrument().selected_technology,
                    ref user_setting.tech_format,
                    ref user_setting.bandwidth,
                    ref user_setting.modtype,
                    ref dir);
                CollectUserSetting();
                pass = ps.KmfMeasureSEM(user_setting,
                                        tps,
                                        ref semResult);


                if (!pass)
                    semResult.result = "Fail";
                else
                    semResult.result = "Pass";


                PublishResult_Sem(true, GetInstrument().selected_technology, GetInstrument().selected_frequency,
                                   semResult, Results);

                UpdateKmfResultEventArgs e = new UpdateKmfResultEventArgs();
                e.MeasType = KMFMEASTYPE.KMF_SEM;
                e.semResult = semResult;

                if (KmfResultUpdate != null)
                    KmfResultUpdate(this, e);

                RunChildSteps(); //If step has child steps.

                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "KMF SEM Failure: " + semResult.errMsg);
                }
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
                Logging(LogLevel.ERROR, "KMF SEM Failure: " + ex.Message);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();

        }

        private void CollectUserSetting()
        {

            user_setting.frequecy = GetInstrument().selected_frequency * 1e6;
            user_setting.averageType = average_type;

            user_setting.triggersrc = triggersource;
            user_setting.triggerdelay = triggerdelay;
            user_setting.triggerlevel = (triggersource == SEMTRIGGERMODE.External) ? triggerlevel_v : triggerlevel_dbm;
            user_setting.averageEnable = averageEnable;
            user_setting.numOfAverage = numOfAverage;

            user_setting.manual_range = manual_range;
            user_setting.range = range;
            user_setting.points = points;
            user_setting.limitConfig = offsetLimit;
            user_setting.configFile = config_file;
        }
    }

    [Display(Name: "SEM Calculation", Groups: new string[] { "PA", "Measurements" })]
    public class PA_SemCalculation : PA_TestStep
    {
        #region Setting
        [Display(Name: "Average", Group: "Measure Setup", Order: 1)]
        [Browsable(true)]
        public bool averageEnable { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 2)]
        [Browsable(true)]
        [EnabledIf("averageEnable", true, HideIfDisabled = true)]
        public int numOfAverage { get; set; }

        [Display(Name: "Average Type", Group: "SEM", Order: 11)]
        [Browsable(true)]
        public AverageType average_type { get; set; }

        [Display(Name: "Manual Power Range", Group: "SEM", Order: 12)]
        [Browsable(true)]
        public bool manual_range { get; set; }

        [Display(Name: "Power Range", Group: "SEM", Order: 13)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("manual_range", true, HideIfDisabled = true)]
        public double range { get; set; }

        [Display(Name: "Points", Group: "Test", Order: 31)]
        [Browsable(false)]
        public int points { get; set; }

        [Display(Name: "Config", Group: "Offset & Limit", Order: 50)]
        [Browsable(true)]
        public KMFLimitConfig offsetLimit { get; set; }

        private string _config_file;
        [Display(Name: "Config File", Group: "Offset & Limit", Order: 51)]
        [EnabledIf("offsetLimit", KMFLimitConfig.Custom, HideIfDisabled = true)]
        [FilePath]
        public string config_file { set; get; }

        #endregion
        private PlugInKMF plugInKmf = null;
        public SEMResult semResult = null;
        private SemUserConfigure user_setting = new SemUserConfigure();

        public PA_SemCalculation()
        {
            averageEnable = false;
            numOfAverage = 10;
            average_type = AverageType.Log;
            points = 8192;
            offsetLimit = KMFLimitConfig.Preset;
            manual_range = false;
            range = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            plugInKmf = GetInstrument().pluginKmf;
        }

        public override void Run()
        {
            bool pass = true;
            semResult = new SEMResult();

            try
            {
                //GetInstrument().pluginKmf.enable_debug = enable_kmf_debug;

                PlugInMeas ps = GetInstrument().Measurement;
                Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();

                MeasureOrNot(true, 999, -999, TestItem_Enum.SEM, tps);
                DIRECTION dir = DIRECTION.UPLINK;

                ps.TestConditionSetup_GetWaveformInfo(GetInstrument().selected_technology,
                    ref user_setting.tech_format,
                    ref user_setting.bandwidth,
                    ref user_setting.modtype,
                    ref dir);
                SemCalcUserSetting();
                pass = ps.KmfMeasureSEM_Calc(user_setting, tps, ref semResult);

                PublishResult_Sem(true, GetInstrument().selected_technology, GetInstrument().selected_frequency,
                                   semResult, Results);

                RunChildSteps(); //If step has child steps.

            }
            catch (Exception ex)
            {
                pass = false;
            }
            finally
            {
                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "KMF SEM Calculation Failure: " + semResult.errMsg);
                }
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }

        private void SemCalcUserSetting()
        {
            user_setting.frequecy = GetInstrument().selected_frequency * 1e6;
            user_setting.averageType = average_type;
            user_setting.averageEnable = averageEnable;
            user_setting.numOfAverage = numOfAverage;
            user_setting.manual_range = manual_range;
            user_setting.range = range;
            user_setting.points = points;
            user_setting.limitConfig = offsetLimit;
            user_setting.configFile = config_file;
        }
    }

    #endregion

    #region XApp Measurements
    [Display(Name: "Launch X-Series App", Groups: new string[] { "PA", "Measurements (X-Series App)" }, Order: 1)]
    public class PA_XAppOpenX : PA_TestStep
    {
        public PA_XAppOpenX()
        {
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            Stopwatch sw = new Stopwatch();
            bool ret = true;
            sw.Reset();
            sw.Start();

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp LAUNCH is asking to check in ivi driver resource for xApp");
                // pa_instrument.xAppM9000Session.Checkin(pa_instrument.xAppResource);
                //pa_instrument.plugInXapp.CheckInResource(pa_instrument.xAppResource);
                GetInstrument().plugInVsaXapp.CheckInResource();
            }
            if (GetInstrument().Measurement.xAppLoadStatus == false)
            {
                Log.Info("xAPP is launching  ...");
                GetInstrument().Measurement.xAppLaunch(GetInstrument().xAppM9000Enable,
                                                     GetInstrument().selected_technology);
                GetInstrument().Measurement.xAppLoadStatus = true;
            }
            RunChildSteps(); //If step has child steps.
            sw.Stop();

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp LAUNCH is done, check out ivi driver resource for PA Solution ");
                //   pa_instrument.xAppM9000Session.Checkout(pa_instrument.xAppResourceName,
                //                                           out pa_instrument.xAppResource);
                GetInstrument().plugInVsaXapp.CheckOutResource(); 
            }

            Logging(LogLevel.INFO, "xApp Launch cost {0}", (double)sw.ElapsedMilliseconds);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            UpgradeVerdict(Verdict.Pass); UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
        }
        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }

    [Display(Name: "EVM X-Series App", Groups: new string[] { "PA", "Measurements (X-Series App)" }, Order: 2)]
    public class PA_EvmMeasurementX : PA_TestStep
    {
        #region Settings
        [Display(Name: "Average State", Group: "Measure Setup", Order: 11)]
        //[EnabledIf("use_recommended_setting", true)]
        public xOnOffMode avgstate { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 12)]
        //[EnabledIf("use_recommended_setting", true)]
        public Int16 avgnumber { get; set; }

        [Display(Name: "Trigger Source", Group: "Measure Setup", Order: 15)]
        public muiTriggerSource trigsource { get; set; }

        [Display(Name: "Trigger RF Level", Group: "Measure Setup", Order: 16)]
        [Unit("dBm")]
        [EnabledIf("trigsource", muiTriggerSource.RFBurst, HideIfDisabled = true)]
        public double triglevel { get; set; }

        [Display(Name: "PXI Trigger Line", Group: "Measure Setup", Order: 17)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        public PXIChassisTriggerEnum PXI_trigger_line { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measure Setup", Order: 18)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        [Unit("s")]
        public double trigger_delay { get; set; }

        private string _xapp_scpi_file;
        [Display(Name: "Additional Scpis File", Group: "Measure Setup", Order: 20)]
        [FilePath]
        public string xapp_scpi_file
        {
            get { return _xapp_scpi_file; }
            set
            {
                _xapp_scpi_file = value;
            }
        }

        [Display(Name: "Analysis Timeslot", Group: "TDSCDMA Measure Setup", Order: 31)]
        public TDTimeSlotType td_slot { get; set; }

        [Display(Name: "HSPA/8PSK Enable", Group: "TDSCDMA Measure Setup", Order: 32)]
        public bool td_enableHspa8Psk { get; set; }


        [Display(Name: "Low Limit (%)", Group: "Cellular Limit", Order: 41)]
        [Browsable(true)]
        public double LowLimit_Cell { get; set; }

        [Display(Name: "High Limit (%)", Group: "Cellular Limit", Order: 42)]
        [Browsable(true)]
        public double HighLimit_Cell { get; set; }

        [Display(Name: "Low Limit (dB)", Group: "WLAN Limit", Order: 43)]
        [Browsable(true)]
        public double LowLimit_WLAN { get; set; }

        [Display(Name: "High Limit (dB)", Group: "WLAN Limit", Order: 44)]
        [Browsable(true)]
        public double HighLimit_WLAN { get; set; }
        #endregion

        private xCommonSetting common_setting = null;
        private xEvmLTESetting lte_setting = null;
        private xTDSDMASetting td_setting = null;
        private object format_setting = null;
        private List<string> scpi_list = new List<string>();
        private int carrier_num = 1;

        private double LowLimit;
        private double HighLimit;

        public PA_EvmMeasurementX()
        {
            trigsource = muiTriggerSource.Immediate;
            triglevel = -20;
            avgstate = xOnOffMode.ON;
            avgnumber = 10;

            td_slot = TDTimeSlotType.TS1;
            td_enableHspa8Psk = true;

            LowLimit_Cell = 0.0;
            HighLimit_Cell = 5.0;

            LowLimit_WLAN = -999;
            HighLimit_WLAN = -40;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret;

            if (!CollectEvmPara())
            {
                UpgradeVerdict(Verdict.Fail);
                return;
            }

            xEvmResult res = new xEvmResult(carrier_num);
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp EVM is asking to check in ivi driver resource for xApp");
                //pa_instrument.plugInXapp.CheckInResource(pa_instrument.xAppResource);
                GetInstrument().plugInVsaXapp.CheckInResource();
            }
            if (GetInstrument().Measurement.xAppLoadStatus == false)
            {
                Log.Info("xAPP is launching ...");
                GetInstrument().Measurement.xAppLaunch(
                                                     GetInstrument().xAppM9000Enable,
                                                     GetInstrument().selected_technology);
                GetInstrument().Measurement.xAppLoadStatus = true;
            }

            ret = GetInstrument().Measurement.xAppMeasureEvm(GetInstrument().selected_technology,
                                                             common_setting,
                                                             format_setting, ref res);
            RunChildSteps(); //If step has child steps.
            sw.Stop();

            if ((LowLimit <= res.carEvmResults[0].rms_evm) &&
                (HighLimit >= res.carEvmResults[0].rms_evm))
            {
                res.result = true;
            }
            else
                res.result = false;

            res.result &= ret;

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp EVM is done, check out ivi driver resource for PA Solution ");
                //pa_instrument.plugInXapp.CheckOutResource(pa_instrument.xAppResourceName,
                //                                        ref pa_instrument.xAppResource);
                GetInstrument().plugInVsaXapp.CheckOutResource();
            }

            if (res.result)
                Logging(LogLevel.INFO, "XApp EVM Result {0}, rms_evm {1}, time cost {2}",
                                        res.result,
                                        res.carEvmResults[0].rms_evm,
                                        (double)sw.ElapsedMilliseconds);
            else
                Logging(LogLevel.ERROR, "XApp EVM Result {0}, rms_evm {1}, time cost {2}",
                                        res.result,
                                        res.carEvmResults[0].rms_evm,
                                        (double)sw.ElapsedMilliseconds);

            for (int i = 1; i < carrier_num; i++)
            {
                Logging(LogLevel.INFO, "XApp EVM carrier{0} rms_evm {1}",
                                        i, res.carEvmResults[i].rms_evm);
            }

            //Results.Publish("xApp EVM Results",
            //    new List<string> { "Result", "rms_evm", "peak_evm", "mag_err", "phase_err", "freq_err", "oof", "rho", "active_ch_num", "time_offset", "trig_to_T0", "power" },
            //    res.result, res.carEvmResults[0].rms_evm, res.carEvmResults[0].peak_evm, res.carEvmResults[0].mag_err, res.carEvmResults[0].phase_err, res.carEvmResults[0].freq_err,
            //    res.carEvmResults[0].oof, res.carEvmResults[0].active_ch_num, res.carEvmResults[0].time_offset, res.carEvmResults[0].trig_to_T0, res.carEvmResults[0].power);

            #region more result
            //switch (selected_tech)
            //{
            //    // To be extended
            //    case SUPPORTEDTECHNOLOGIES.LTE5MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTE10MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTE20MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTE20_20MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTETDD10MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTETDD20MHZ:
            //    case SUPPORTEDTECHNOLOGIES.LTETDD5MHZ:
            //        Logging(LogLevel.DEBUG, "rms_evm            peak_evm            mag_err            phase_err            freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].rms_evm, res.carEvmResults[0].peak_evm, res.carEvmResults[0].mag_err, res.carEvmResults[0].phase_err, res.carEvmResults[0].freq_err);
            //        Logging(LogLevel.DEBUG, "max_rms_evm        max_peak_evm        max_mag_err        max_phase_err        max_freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].max_rms_evm, res.carEvmResults[0].max_peak_evm, res.carEvmResults[0].max_mag_err, res.carEvmResults[0].max_phase_err, res.carEvmResults[0].max_freq_err);
            //        Logging(LogLevel.DEBUG, "oof                rho                 active_ch_num      time_offset          trig_to_T0            power");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}              {4}",
            //           res.carEvmResults[0].oof, res.carEvmResults[0].rho, res.carEvmResults[0].active_ch_num, res.carEvmResults[0].time_offset, res.carEvmResults[0].trig_to_T0, res.carEvmResults[0].power);
            //        break;
            //    // To be extended
            //    case SUPPORTEDTECHNOLOGIES.WLANN20MHZ:
            //    case SUPPORTEDTECHNOLOGIES.WLANN40MHZ:
            //    case SUPPORTEDTECHNOLOGIES.WLANAC40MHZ:
            //    case SUPPORTEDTECHNOLOGIES.WLANAC80MHZ:
            //    case SUPPORTEDTECHNOLOGIES.WLANAC160MHZ:
            //        Logging(LogLevel.DEBUG, "rms_evm            peak_evm            mag_err            phase_err            freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].rms_evm, res.carEvmResults[0].peak_evm, res.carEvmResults[0].mag_err, res.carEvmResults[0].phase_err, res.carEvmResults[0].freq_err);
            //        Logging(LogLevel.DEBUG, "max_rms_evm        max_peak_evm        max_mag_err        max_phase_err        max_freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].max_rms_evm, res.carEvmResults[0].max_peak_evm, res.carEvmResults[0].max_mag_err, res.carEvmResults[0].max_phase_err, res.carEvmResults[0].max_freq_err);
            //        Logging(LogLevel.DEBUG, "oof                rho                 active_ch_num      time_offset          trig_to_T0            power");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}              {4}",
            //           res.carEvmResults[0].oof, res.carEvmResults[0].rho, res.carEvmResults[0].active_ch_num, res.carEvmResults[0].time_offset, res.carEvmResults[0].trig_to_T0, res.carEvmResults[0].power);
            //        break;
            //    default:
            //        Logging(LogLevel.DEBUG, "rms_evm            peak_evm            mag_err            phase_err            freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].rms_evm, res.carEvmResults[0].peak_evm, res.carEvmResults[0].mag_err, res.carEvmResults[0].phase_err, res.carEvmResults[0].freq_err);
            //        Logging(LogLevel.DEBUG, "max_rms_evm        max_peak_evm        max_mag_err        max_phase_err        max_freq_err");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}",
            //           res.carEvmResults[0].max_rms_evm, res.carEvmResults[0].max_peak_evm, res.carEvmResults[0].max_mag_err, res.carEvmResults[0].max_phase_err, res.carEvmResults[0].max_freq_err);
            //        Logging(LogLevel.DEBUG, "oof                rho                 active_ch_num      time_offset          trig_to_T0            power");
            //        Logging(LogLevel.DEBUG, "{0}        {1}           {2}           {3}           {4}              {4}",
            //           res.carEvmResults[0].oof, res.carEvmResults[0].rho, res.carEvmResults[0].active_ch_num, res.carEvmResults[0].time_offset, res.carEvmResults[0].trig_to_T0, res.carEvmResults[0].power);
            //        break;
            //}
            #endregion
            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            // UpgradeVerdict(Verdict.Pass);
            UpgradeVerdict(res.result ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }

        public bool CollectEvmPara()
        {
            SUPPORTEDFORMAT tf = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology);

            if ((tf == SUPPORTEDFORMAT.WLAN11AX) ||
                (tf == SUPPORTEDFORMAT.GSM))
            {
                Logging(LogLevel.ERROR, "EVM is not supported with current technology {0}\n", GetInstrument().selected_technology);
                return false;
            }

            if (File.Exists(_xapp_scpi_file))
                PlugInXApp.ParseXappScpiFile(_xapp_scpi_file, ref scpi_list);

            common_setting = new xCommonSetting(avgstate, avgnumber, 
                                                trigsource, triglevel, 
                                                PXI_trigger_line, trigger_delay,scpi_list);
            carrier_num = 1;
            switch (tf)
            {
                case SUPPORTEDFORMAT.TDSCDMA:
                    td_setting = new xTDSDMASetting(td_slot, td_enableHspa8Psk);
                    format_setting = (object)td_setting;

                    break;

                case SUPPORTEDFORMAT.LTEAFDD:
                case SUPPORTEDFORMAT.LTEATDD:
                    lte_setting = new xEvmLTESetting();
                    lte_setting.carriernum = GetInstrument().vsa.INIInfo.CarrierCount;
                    lte_setting.cc_offset = GetInstrument().vsa.INIInfo.CCOffsets;
                                
                    format_setting = (object)lte_setting;
                    carrier_num = lte_setting.carriernum;

                    break;
                default:
                    format_setting = null;
                    break;
            }

            if (tf == SUPPORTEDFORMAT.WLAN11N || tf == SUPPORTEDFORMAT.WLAN11AC)
            {
                LowLimit = LowLimit_WLAN;
                HighLimit = HighLimit_WLAN;
            }
            else
            {
                LowLimit = LowLimit_Cell;
                HighLimit = HighLimit_Cell;
            }
            return true;
        }
    }

    [Display(Name: "SEM X-Series App", Groups: new string[] { "PA", "Measurements (X-Series App)" }, Order: 3)]
    public class PA_SemMeasurementX : PA_TestStep
    {
        #region Settings
        [Display(Name: "Average State", Group: "Measure Setup", Order: 10)]
        public xOnOffMode avgstate { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 11)]
        public Int16 avgnumber { get; set; }

        [Display(Name: "Trigger Source", Group: "Measure Setup", Order: 16)]
        public muiTriggerSource trigsource { get; set; }

        [Display(Name: "Trigger RF Level", Group: "Measure Setup", Order: 17)]
        [Unit("dBm")]
        [EnabledIf("trigsource", muiTriggerSource.RFBurst, HideIfDisabled = true)]
        public double triglevel { get; set; }

        [Display(Name: "PXI Trigger Line", Group: "Measure Setup", Order: 18)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        public PXIChassisTriggerEnum PXI_trigger_line { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measure Setup", Order: 19)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        [Unit("s")]
        public double trigger_delay { get; set; }

        private string _xapp_scpi_file;
        [Display(Name: "Additional Scpis File", Group: "Measure Setup", Order: 20)]
        [FilePath]
        public string xapp_scpi_file
        {
            get { return _xapp_scpi_file; }
            set
            {
                _xapp_scpi_file = value;
            }
        }

        [Display(Name: "Analysis Timeslot", Group: "TDSCDMA Measure Setup", Order: 31)]
        public TDTimeSlotType td_slot { get; set; }

        [Display(Name: "HSPA/8PSK Enable", Group: "TDSCDMA Measure Setup", Order: 32)]
        public bool td_enableHspa8Psk { get; set; }
        #endregion

        private xCommonSetting common_setting = null;
        private xTDSDMASetting td_setting = null;
        private object format_setting = null;
        private List<string> scpi_list = new List<string>();

        public PA_SemMeasurementX()
        {
            trigsource = muiTriggerSource.Immediate;
            triglevel = -20;
            avgstate = xOnOffMode.ON;
            avgnumber = 10;

            td_slot = TDTimeSlotType.TS1;
            td_enableHspa8Psk = true;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret;
            xSemResult res = new xSemResult(-9.99e2);
            xSemResultPassFail resPass = new xSemResultPassFail();
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            if (!CollectSemPara())
            {
                UpgradeVerdict(Verdict.Fail);
                return;
            }

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp SEM is asking to check in ivi driver resource for xApp");
                GetInstrument().plugInVsaXapp.CheckInResource();
            }
            if (GetInstrument().Measurement.xAppLoadStatus == false)
            {
                Log.Info("xAPP is launching ...");
                GetInstrument().Measurement.xAppLaunch(
                                                     GetInstrument().xAppM9000Enable,
                                                     GetInstrument().selected_technology);

                GetInstrument().Measurement.xAppLoadStatus = true;
            }
            ret = GetInstrument().Measurement.xAppMeasureSem(GetInstrument().selected_technology,
                                                           common_setting, format_setting,
                                                           ref res, ref resPass);

            res.result = ret && resPass.pass;

            RunChildSteps(); //If step has child steps.
            sw.Stop();

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp SEM is done, check out ivi driver resource for PA Solution ");
                GetInstrument().plugInVsaXapp.CheckOutResource();
            }
            Logging(LogLevel.INFO, "XApp SEM Result {0}, time cost {1}",
                                   res.result,
                                   (double)sw.ElapsedMilliseconds);

            Results.Publish("xApp SEM Results",
                        new List<string> { "Results" }, res.result.ToString());

            SUPPORTEDFORMAT tf = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology);
            ShowSemResult(tf, res);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
            // UpgradeVerdict(Verdict.Pass);
            UpgradeVerdict(res.result ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
        public bool CollectSemPara()
        {
            SUPPORTEDFORMAT tf = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology);

            if (tf == SUPPORTEDFORMAT.WLAN11AX)
            {
                Logging(LogLevel.ERROR, "Sem is not supported with current technology {0}\n",
                                         GetInstrument().selected_technology);
                return false;
            }

            if ((tf == SUPPORTEDFORMAT.GSM) ||
                    (tf == SUPPORTEDFORMAT.EDGE))
            {
                Logging(LogLevel.ERROR, "SEM is not supported with GSM/EDGE!");
                return false;
            }

            if (File.Exists(_xapp_scpi_file))
                PlugInXApp.ParseXappScpiFile(_xapp_scpi_file, ref scpi_list);

            common_setting = new xCommonSetting(avgstate, avgnumber,
                                                trigsource, triglevel,
                                                PXI_trigger_line, trigger_delay, scpi_list);

            if (tf == SUPPORTEDFORMAT.TDSCDMA)
            {
                td_setting = new xTDSDMASetting(td_slot, td_enableHspa8Psk);
                format_setting = td_setting;
            }
            else
                format_setting = null;

            return true;
        }

        private void ShowSemResult(SUPPORTEDFORMAT tech, xSemResult res)
        {
            switch (tech) // Assume that Meas Type: Total Power Reference
            {
                #region not use
                //case "LTEAFDD":
                //case "LTEATDD":
                //case "WLAN":
                //case "MSR":

                //    Logging(LogLevel.DEBUG, "Result, Total Pwr (dBm), Ref/Left Ref Pwr (dBm/Hz), Rigth Ref Pwr (dBm/Hz), Ref/Left Peak Freq (Hz), Rigth Peak Freq (Hz)");
                //    Logging(LogLevel.DEBUG, " {0}    {1}              {2}                        {3}                    {4}",
                //                              res.result, res.left_power, res.right_power, res.left_peakfreq, res.right_peakfreq);

                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negA (dBc)   Abs Avg Pwr negA (dBm/Hz)   Rel Peak Pwr negA(dBc)   Abs Peak Pwr negA(dBm / Hz)     Peak Frequency negA(Hz) ");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.A.neg_relpower, res.A.neg_abspower, res.A.neg_relpeakpower, res.A.neg_abspeakpower, res.A.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posA (dBc)     Abs Avg Pwr posA (dBm/Hz)      Rel Peak Pwr posA(dBc)     Abs Peak Pwr posA(dBm / Hz)     Peak Frequency posA(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.A.pos_relpower, res.A.pos_abspower, res.A.pos_relpeakpower, res.A.pos_abspeakpower, res.A.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negB (dBc)     Abs Avg Pwr negB (dBm/Hz)      Rel Peak Pwr negB(dBc)     Abs Peak Pwr negB(dBm / Hz)     Peak Frequency negB(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.B.neg_relpower, res.B.neg_abspower, res.B.neg_relpeakpower, res.B.neg_abspeakpower, res.B.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posB (dBc)     Abs Avg Pwr posB (dBm/Hz)      Rel Peak Pwr posB(dBc)     Abs Peak Pwr posB(dBm / Hz)     Peak Frequency posB(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.B.pos_relpower, res.B.pos_abspower, res.B.pos_relpeakpower, res.B.pos_abspeakpower, res.B.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negC (dBc)     Abs Avg Pwr negC (dBm/Hz)      Rel Peak Pwr negC(dBc)     Abs Peak Pwr negC(dBm / Hz)     Peak Frequency negC(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.C.neg_relpower, res.C.neg_abspower, res.C.neg_relpeakpower, res.C.neg_abspeakpower, res.C.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posC (dBc)     Abs Avg Pwr posC (dBm/Hz)      Rel Peak Pwr posC(dBc)     Abs Peak Pwr posC(dBm / Hz)     Peak Frequency posC(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.C.pos_relpower, res.C.pos_abspower, res.C.pos_relpeakpower, res.C.pos_abspeakpower, res.C.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negD (dBc)     Abs Avg Pwr negD (dBm/Hz)      Rel Peak Pwr negD(dBc)     Abs Peak Pwr negD(dBm / Hz)     Peak Frequency negD(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.D.neg_relpower, res.D.neg_abspower, res.D.neg_relpeakpower, res.D.neg_abspeakpower, res.D.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posD (dBc)     Abs Avg Pwr posD (dBm/Hz)      Rel Peak Pwr posD(dBc)     Abs Peak Pwr posD(dBm / Hz)     Peak Frequency posD(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.D.pos_relpower, res.D.pos_abspower, res.D.pos_relpeakpower, res.D.pos_abspeakpower, res.D.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negE (dBc)     Abs Avg Pwr negE (dBm/Hz)      Rel Peak Pwr negE(dBc)     Abs Peak Pwr negE(dBm / Hz)     Peak Frequency negE(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.E.neg_relpower, res.E.neg_abspower, res.E.neg_relpeakpower, res.E.neg_abspeakpower, res.E.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posE (dBc)     Abs Avg Pwr posE (dBm/Hz)      Rel Peak Pwr posE(dBc)     Abs Peak Pwr posE(dBm / Hz)     Peak Frequency posE(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.E.pos_relpower, res.E.pos_abspower, res.E.pos_relpeakpower, res.E.pos_abspeakpower, res.E.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negF (dBc)     Abs Avg Pwr negF (dBm/Hz)      Rel Peak Pwr negF(dBc)     Abs Peak Pwr negF(dBm / Hz)     Peak Frequency negF(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.F.neg_relpower, res.F.neg_abspower, res.F.neg_relpeakpower, res.F.neg_abspeakpower, res.F.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posF (dBc)     Abs Avg Pwr posF (dBm/Hz)      Rel Peak Pwr posF(dBc)     Abs Peak Pwr posF(dBm / Hz)     Peak Frequency posF(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.F.pos_relpower, res.F.pos_abspower, res.F.pos_relpeakpower, res.F.pos_abspeakpower, res.F.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Min Margin negA (dB) ,  Min Margin posA (dB) ,  Min Margin negB (dB) ,  Min Margin posB (dB) ,  Min Margin negC (dB) ,  Min Margin posC (dB)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.negA_margin, res.posA_margin, res.negB_margin, res.posB_margin, res.negC_margin, res.posC_margin);
                //    Logging(LogLevel.DEBUG, "Min Margin negD (dB) ,  Min Margin posD (dB) ,  Min Margin negE (dB) ,  Min Margin posE (dB) ,  Min Margin negF (dB) ,  Min Margin posF (dB)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}                        {5}", res.negD_margin, res.posD_margin, res.negE_margin, res.posE_margin, res.negF_margin, res.posF_margin);
                //    break;
                //default:
                //    Logging(LogLevel.DEBUG, "Result       Absolute Pwr (dBm)          Peak Reference Freq (Hz)");
                //    Logging(LogLevel.DEBUG, " {0}     {1}           {2}", res.result, res.cf_power, res.cf_peakfreq);

                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negA (dBc)   Abs Avg Pwr negA (dBm/Hz)   Rel Peak Pwr negA(dBc)   Abs Peak Pwr negA(dBm / Hz)     Peak Frequency negA(Hz) ");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.A.neg_relpower, res.A.neg_abspower, res.A.neg_relpeakpower, res.A.neg_abspeakpower, res.A.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posA (dBc)     Abs Avg Pwr posA (dBm/Hz)      Rel Peak Pwr posA(dBc)     Abs Peak Pwr posA(dBm / Hz)     Peak Frequency posA(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.A.pos_relpower, res.A.pos_abspower, res.A.pos_relpeakpower, res.A.pos_abspeakpower, res.A.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negB (dBc)     Abs Avg Pwr negB (dBm/Hz)      Rel Peak Pwr negB(dBc)     Abs Peak Pwr negB(dBm / Hz)     Peak Frequency negB(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.B.neg_relpower, res.B.neg_abspower, res.B.neg_relpeakpower, res.B.neg_abspeakpower, res.B.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posB (dBc)     Abs Avg Pwr posB (dBm/Hz)      Rel Peak Pwr posB(dBc)     Abs Peak Pwr posB(dBm / Hz)     Peak Frequency posB(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.B.pos_relpower, res.B.pos_abspower, res.B.pos_relpeakpower, res.B.pos_abspeakpower, res.B.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negC (dBc)     Abs Avg Pwr negC (dBm/Hz)      Rel Peak Pwr negC(dBc)     Abs Peak Pwr negC(dBm / Hz)     Peak Frequency negC(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.C.neg_relpower, res.C.neg_abspower, res.C.neg_relpeakpower, res.C.neg_abspeakpower, res.C.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posC (dBc)     Abs Avg Pwr posC (dBm/Hz)      Rel Peak Pwr posC(dBc)     Abs Peak Pwr posC(dBm / Hz)     Peak Frequency posC(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.C.pos_relpower, res.C.pos_abspower, res.C.pos_relpeakpower, res.C.pos_abspeakpower, res.C.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negD (dBc)     Abs Avg Pwr negD (dBm/Hz)      Rel Peak Pwr negD(dBc)     Abs Peak Pwr negD(dBm / Hz)     Peak Frequency negD(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.D.neg_relpower, res.D.neg_abspower, res.D.neg_relpeakpower, res.D.neg_abspeakpower, res.D.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posD (dBc)     Abs Avg Pwr posD (dBm/Hz)      Rel Peak Pwr posD(dBc)     Abs Peak Pwr posD(dBm / Hz)     Peak Frequency posD(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.D.pos_relpower, res.D.pos_abspower, res.D.pos_relpeakpower, res.D.pos_abspeakpower, res.D.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negE (dBc)     Abs Avg Pwr negE (dBm/Hz)      Rel Peak Pwr negE(dBc)     Abs Peak Pwr negE(dBm / Hz)     Peak Frequency negE(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.E.neg_relpower, res.E.neg_abspower, res.E.neg_relpeakpower, res.E.neg_abspeakpower, res.E.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posE (dBc)     Abs Avg Pwr posE (dBm/Hz)      Rel Peak Pwr posE(dBc)     Abs Peak Pwr posE(dBm / Hz)     Peak Frequency posE(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.E.pos_relpower, res.E.pos_abspower, res.E.pos_relpeakpower, res.E.pos_abspeakpower, res.E.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr negF (dBc)     Abs Avg Pwr negF (dBm/Hz)      Rel Peak Pwr negF(dBc)     Abs Peak Pwr negF(dBm / Hz)     Peak Frequency negF(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.F.neg_relpower, res.F.neg_abspower, res.F.neg_relpeakpower, res.F.neg_abspeakpower, res.F.neg_peakfreq);
                //    Logging(LogLevel.DEBUG, "Rel Avg Pwr posF (dBc)     Abs Avg Pwr posF (dBm/Hz)      Rel Peak Pwr posF(dBc)     Abs Peak Pwr posF(dBm / Hz)     Peak Frequency posF(Hz)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.F.pos_relpower, res.F.pos_abspower, res.F.pos_relpeakpower, res.F.pos_abspeakpower, res.F.pos_peakfreq);
                //    Logging(LogLevel.DEBUG, "Min Margin negA (dB) ,  Min Margin posA (dB) ,  Min Margin negB (dB) ,  Min Margin posB (dB) ,  Min Margin negC (dB) ,  Min Margin posC (dB)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}", res.negA_margin, res.posA_margin, res.negB_margin, res.posB_margin, res.negC_margin, res.posC_margin);
                //    Logging(LogLevel.DEBUG, "Min Margin negD (dB) ,  Min Margin posD (dB) ,  Min Margin negE (dB) ,  Min Margin posE (dB) ,  Min Margin negF (dB) ,  Min Margin posF (dB)");
                //    Logging(LogLevel.DEBUG, " {0}                {1}                    {2}                 {3}                        {4}                       {5}", res.negD_margin, res.posD_margin, res.negE_margin, res.posE_margin, res.negF_margin, res.posF_margin);
                //    break;
                #endregion
                case SUPPORTEDFORMAT.LTEAFDD:
                case SUPPORTEDFORMAT.LTEATDD:
                case SUPPORTEDFORMAT.WLAN11N:
                case SUPPORTEDFORMAT.WLAN11AC:
                    Logging(LogLevel.DEBUG, "Result, Total Pwr (dBm), Ref/Left Ref Pwr (dBm/Hz), Rigth Ref Pwr (dBm/Hz), Ref/Left Peak Freq (Hz), Rigth Peak Freq (Hz)");
                    Logging(LogLevel.DEBUG, " {0}    {1}              {2}                        {3}                    {4}",
                                              res.result, res.left_power, res.right_power, res.left_peakfreq, res.right_peakfreq);
                    break;
                default:
                    Logging(LogLevel.DEBUG, "Result       Absolute Pwr (dBm)          Peak Reference Freq (Hz)");
                    Logging(LogLevel.DEBUG, " {0}     {1}           {2}", res.result, res.cf_power, res.cf_peakfreq);
                    break;
            }

            Logging(LogLevel.DEBUG, "Offset  Rel Avg Pwr neg (dBc)   Abs Avg Pwr neg (dBm/Hz)   Rel Peak Pwr neg(dBc)   Abs Peak Pwr neg(dBm / Hz)     Peak Frequency neg(Hz)  Rel Avg Pwr posA (dBc)     Abs Avg Pwr pos (dBm/Hz)      Rel Peak Pwr pos(dBc)     Abs Peak Pwr pos(dBm / Hz)     Peak Frequency pos(Hz)");
            Logging(LogLevel.DEBUG, "A       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                                        res.A.neg_relpower, res.A.neg_abspower, res.A.neg_relpeakpower, res.A.neg_abspeakpower, res.A.neg_peakfreq,
                                        res.A.pos_relpower, res.A.pos_abspower, res.A.pos_relpeakpower, res.A.pos_abspeakpower, res.A.pos_peakfreq);

            Logging(LogLevel.DEBUG, "B       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                                        res.B.neg_relpower, res.B.neg_abspower, res.B.neg_relpeakpower, res.B.neg_abspeakpower, res.B.neg_peakfreq,
                                        res.B.pos_relpower, res.B.pos_abspower, res.B.pos_relpeakpower, res.B.pos_abspeakpower, res.B.pos_peakfreq);

            Logging(LogLevel.DEBUG, "C       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                                        res.C.neg_relpower, res.C.neg_abspower, res.C.neg_relpeakpower, res.C.neg_abspeakpower, res.C.neg_peakfreq,
                                        res.C.pos_relpower, res.C.pos_abspower, res.C.pos_relpeakpower, res.C.pos_abspeakpower, res.C.pos_peakfreq);

            Logging(LogLevel.DEBUG, "D       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                                        res.D.neg_relpower, res.D.neg_abspower, res.D.neg_relpeakpower, res.D.neg_abspeakpower, res.D.neg_peakfreq,
                                        res.D.pos_relpower, res.D.pos_abspower, res.D.pos_relpeakpower, res.D.pos_abspeakpower, res.D.pos_peakfreq);

            Logging(LogLevel.DEBUG, "E       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                    res.E.neg_relpower, res.E.neg_abspower, res.E.neg_relpeakpower, res.E.neg_abspeakpower, res.E.neg_peakfreq,
                    res.E.pos_relpower, res.E.pos_abspower, res.E.pos_relpeakpower, res.E.pos_abspeakpower, res.E.pos_peakfreq);

            Logging(LogLevel.DEBUG, "F       {0}                {1}                    {2}                 {3}                        {4}                    {5}                {6}                    {7}                 {8}                        {9}",
                                        res.F.neg_relpower, res.F.neg_abspower, res.F.neg_relpeakpower, res.F.neg_abspeakpower, res.F.neg_peakfreq,
                                        res.F.pos_relpower, res.F.pos_abspower, res.F.pos_relpeakpower, res.F.pos_abspeakpower, res.F.pos_peakfreq);

            Logging(LogLevel.DEBUG, "Offset  Min Margin neg (dB) ,  Min Margin pos (dB)");
            Logging(LogLevel.DEBUG, "A  {0}                {1}",
                                        res.negA_margin, res.posA_margin);
            Logging(LogLevel.DEBUG, "B  {0}                {1}",
                                        res.negB_margin, res.posB_margin);
            Logging(LogLevel.DEBUG, "C  {0}                {1}",
                                        res.negC_margin, res.posC_margin);
            Logging(LogLevel.DEBUG, "D  {0}                {1}",
                                        res.negD_margin, res.posD_margin);
            Logging(LogLevel.DEBUG, "E  {0}                {1}",
                                        res.negE_margin, res.posE_margin);
            Logging(LogLevel.DEBUG, "F  {0}                {1}",
                                        res.negF_margin, res.posF_margin);
        }
    }

    [Display(Name: "ORFS X-Series App", Groups: new string[] { "PA", "Measurements (X-Series App)" }, Order: 4)]
    public class PA_OrfsMeasurementX : PA_TestStep
    {

        // share between single and multi offset
        #region Settings
        [Display(Name: "Meas Type", Group: "ORFS Measure Setup", Order: 1)]
        public ORFSTYPE meastype { get; set; }

        [Display(Name: "Meas Method", Group: "ORFS Measure Setup", Order: 2)]
        public muiOrfsMethod measmethod { get; set; }

        [Display(Name: "Multi Offset Freq List", Group: "ORFS Measure Setup", Order: 3)]
        [EnabledIf("measmethod", muiOrfsMethod.MultiOffset)]
        public muiOrfsFreqList freqlist { get; set; }

        [Display(Name: "Average State", Group: "Measure Setup", Order: 11)]
        public xOnOffMode avgstate { get; set; }

        [Display(Name: "Average Number", Group: "Measure Setup", Order: 12)]
        public Int16 avgnumber { get; set; }

        [Display(Name: "Trigger Source", Group: "Measure Setup", Order: 21)]
        public muiTriggerSource trigsource { get; set; }

        [Display(Name: "Trigger RF Level", Group: "Measure Setup", Order: 22)]
        [Unit("dBm")]
        [EnabledIf("trigsource", muiTriggerSource.RFBurst, HideIfDisabled = true)]
        public double triglevel { get; set; }

        [Display(Name: "PXI Trigger Line", Group: "Measure Setup", Order: 23)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        public PXIChassisTriggerEnum PXI_trigger_line { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measure Setup", Order: 24)]
        [EnabledIf("trigsource", muiTriggerSource.PXI, HideIfDisabled = true)]
        [Unit("s")]
        public double trigger_delay { get; set; }

        ////[Browsable(false)]
        //public xOrfsResult result { get; set; }

        private string _xapp_scpi_file;
        [Display(Name: "Additional Scpis File", Group: "Measure Setup", Order: 31)]
        [FilePath]
        public string xapp_scpi_file
        {
            get { return _xapp_scpi_file; }
            set
            {
                _xapp_scpi_file = value;
            }
        }

        #endregion

        private xCommonSetting common_setting = null;
        private xOrfsSetting orfs_setting = null;
        private List<string> scpi_list = new List<string>();
        public PA_OrfsMeasurementX()
        {
            avgstate = xOnOffMode.ON;
            avgnumber = 20;
            meastype = ORFSTYPE.Modulation;
            measmethod = muiOrfsMethod.MultiOffset;
            trigsource = muiTriggerSource.RFBurst;
            triglevel = -20;
            freqlist = muiOrfsFreqList.StandardList;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            bool ret;
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            if (!CollectOrfsPara())
            {
                UpgradeVerdict(Verdict.Fail);
                return;
            }
            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp ORFS is asking to check in ivi driver resource for xApp");
                GetInstrument().plugInVsaXapp.CheckInResource();
            }
            if (GetInstrument().Measurement.xAppLoadStatus == false)
            {
                Log.Info("xAPP is launching M90XA ...");
                GetInstrument().Measurement.xAppLaunch(
                                                     GetInstrument().xAppM9000Enable,
                                                     GetInstrument().selected_technology);
                GetInstrument().Measurement.xAppLoadStatus = true;
            }
            xOrfsResult result = new xOrfsResult();
            int offset_num = 14; // tbd
            if (measmethod == muiOrfsMethod.MultiOffset) //hxiao, to be change for array list
            {
                if (freqlist == muiOrfsFreqList.StandardList)
                    offset_num = 13;
                else if (freqlist == muiOrfsFreqList.ShortList)
                    offset_num = 6;
                else if (freqlist == muiOrfsFreqList.LimitList)
                    offset_num = 13;
                else
                    offset_num = 14;
            }

            OrfsxAppResult res = new OrfsxAppResult();

            result.bPassed = true;
            result.meas_type = meastype;
            result.method = measmethod;

            //res = GetInstrument().Measurement.xAppMeasureOrfs(technology, radiodevice, trigsource, freq, ref_pwr, offsetnumber,
            //freqlist, avgstate, avgnumber, meastype, modavgtype, measmethod, refcarrier, measureregion, modmeasbwstate,
            //modcarrierrbw, modinner18krbw, modout18krbw, modout60krbw, switchcarrierrbw, switchinner18krbw, switchout18krbw,
            //improductorder, exceptionstate, exceptrule, phasenoiseopt, bandextension, ref measresult);
            ret = GetInstrument().Measurement.xAppMeasureOrfs(GetInstrument().selected_technology,
                                                            common_setting, orfs_setting,
                                                            ref res);
            result.meas_result = res;
            result.bPassed = ret;

            RunChildSteps(); //If step has child steps.
            sw.Stop();

            if (GetInstrument().xAppM9000Enable == true)
            {
                Log.Info("XApp ORFS is done, check out ivi driver resource for PA Solution ");
                // pretend to check out, but not to assing vsa actaully 
                GetInstrument().plugInVsaXapp.CheckOutResource();
            }
            Logging(LogLevel.INFO, "PA_OrfsMeasurementX Result {0}, time cost {1}",
                                   result.bPassed, (double)sw.ElapsedMilliseconds);

            Results.Publish("xApp orfs Results",
                        new List<string> { "Results" }, result.bPassed.ToString());
            //if (measmethod == muiOrfsMethod.MultiOffset) //hxiao, to be change for array list
            //{
            //    Results.Publish("xApp ORFS Multi Offset",
            //        new List<string> { "Result", "Low offset Pwr (dB)", "Low offset Pwr (dBm)", "Low offset Pwr (dB)", "Low offset Pwr (dBm)" },
            //        ret, res.offset_result[0], res.offset_result[1], res.offset_result[2], res.offset_result[3]);
            //}
            //else if (measmethod == muiOrfsMethod.SingleOffset) //hxiao revisit, publich all offset
            //{
            //    Results.Publish("xApp ORFS Single Offset",
            //        new List<string> { "Result", "Freq", "Offset", "Power", "DeltaLimit", "DeltaReference" },
            //                            ret, res.swept_frequency, res.swept_freq_offset, res.swept_abs_pwr,
            //                            res.swept_delta_2limit, res.swept_delta_2ref);
            //}
            //else
            //{
            //    Results.Publish("xApp ORFS Full Frame",
            //        new List<string> { "Result", "Freq", "Offset", "Power", "DeltaLimit", "DeltaReference" },
            //                            ret, res.swept_frequency, res.swept_freq_offset, res.swept_abs_pwr,
            //                            res.swept_delta_2limit, res.swept_delta_2ref);
            //}

            ShowORFSResult(result, offset_num);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }

        private bool CollectOrfsPara()
        {
            SUPPORTEDFORMAT tech_format = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology);
            if ((tech_format != SUPPORTEDFORMAT.EDGE) &&
                (tech_format != SUPPORTEDFORMAT.GSM))
            {
                Logging(LogLevel.ERROR, "ORFS is not supported with current technology\n");
                return false;
            }

            if (File.Exists(_xapp_scpi_file))
                PlugInXApp.ParseXappScpiFile(_xapp_scpi_file, ref scpi_list);

            common_setting = new xCommonSetting(avgstate, avgnumber,
                                                trigsource, triglevel,
                                                PXI_trigger_line, trigger_delay, scpi_list);

            orfs_setting = new xOrfsSetting();
            orfs_setting.meastype = meastype;
            orfs_setting.measmethod = measmethod;
            orfs_setting.freqlist = freqlist;

            return true;
        }
        private void ShowORFSResult(xOrfsResult result, int offset_num)
        {
            OrfsxAppResult res = result.meas_result;

            Logging(LogLevel.INFO, "XAPP ORFS return {0}", result.bPassed.ToString());

            if ((result.method == muiOrfsMethod.SingleOffset) &&
                    (result.meas_type != ORFSTYPE.FullFrame))
            {
                Logging(LogLevel.DEBUG, "Modulation spectrum power {0} dB,  {1} dBm", result.meas_result.mod_rel_power, result.meas_result.mod_abs_power);
                Logging(LogLevel.DEBUG, "Switching transient  power {0} dB,  {1} dBm", result.meas_result.switch_rel_power, result.meas_result.switch_abs_power);

            }
            if ((result.method == muiOrfsMethod.Swept) &&
                (result.meas_type == ORFSTYPE.Modulation))
            {
                Logging(LogLevel.DEBUG, "Frequency  {0},  Offset frequency from carrier frequency {1}",
                                        result.meas_result.swept_frequency,
                                        result.meas_result.swept_freq_offset);

                Logging(LogLevel.DEBUG, "Power {0} dBm,  delta from limit {1} dB, delta from reference {2} dB",
                                        result.meas_result.swept_abs_pwr,
                                        result.meas_result.swept_delta_2limit,
                                        result.meas_result.swept_delta_2ref);

            }

            if (result.meas_result.offset_result == null)
                return;

            if (result.meas_type != ORFSTYPE.Switching)
            {
                Logging(LogLevel.DEBUG, "Modulation Offset result\n");

                Logging(LogLevel.DEBUG, "Offset  Low offset(dB)   Low Offset(dBm)    Upper Offset(dB)    Upper Offset(dBm)");
                for (int i = 1; i < offset_num + 1; i++)
                    Logging(LogLevel.DEBUG, "{0}  {1}           {2}           {3}           {4}",
                                            i,
                                            res.offset_result[4 * i],
                                            res.offset_result[4 * i + 1],
                                            res.offset_result[4 * i + 2],
                                            res.offset_result[i * 4 + 3]);
            }

            if ((result.meas_type == ORFSTYPE.ModulationAndSwitching) ||
                (result.meas_type == ORFSTYPE.Switching))
            {
                Logging(LogLevel.DEBUG, "Sqitching Offset result\n");

                Logging(LogLevel.DEBUG, "Offset  Low offset(dB)   Low Offset(dBm)    Upper Offset(dB)    Upper Offset(dBm)");
                for (int i = 16; i < 19; i++)
                    Logging(LogLevel.DEBUG, "{0}  {1}           {2}           {3}           {4}",
                                            i - 15,
                                            res.offset_result[4 * i],
                                            res.offset_result[4 * i + 1],
                                            res.offset_result[4 * i + 2],
                                            res.offset_result[i * 4 + 3]);
            }

        }
    }
    #endregion

    #region ET/DPD Related

    public abstract class PA_EtDpdCommon : PA_TestStep
    {
        private const string SIG_STUDIO_TEMP_FILE_PATH = @"C:\Temp\";

        public PlugInMeas meas;
        public ISigStudio ss;
        public IUserDefinedDPD ud_dpd;
        public IAWG awg;
        public IVSG vsg;
        public IVSA vsa;

        public PA_EtDpdCommon()
        {
            meas = null;
            ss = null;
            awg = null;
            vsg = null;
            vsa = null;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
        }

        public override void Run()
        {
            base.Run();
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }

        public bool Init()
        {
            bool ret = true;
            try
            {
                if (Common.LicenseManager.CreateResult != LicensingCreateResult.SUCCESS)
                    Common.LicenseManager.Create();

                bool s8903a_lic = Common.LicenseManager.Instance.IsLicensed(Common.LicenseManager.ETnDPD_license);

                if (s8903a_lic == false)
                {
                    throw new Exception("ET/DPD measurement not licensed!");
                }

                if (GetInstrument() == null)
                {
                    throw new Exception("PA Instrument not initialized!");
                }

                meas = GetInstrument().Measurement;
                if (meas == null)
                {
                    throw new Exception("Measurement not initialized!");
                }

                ss = GetInstrument().sigStudio;
                ud_dpd = GetInstrument().sigStudio as IUserDefinedDPD;
                if (ss == null)
                {
                    throw new Exception("SigStudio not initialized!");
                }

                awg = GetInstrument().awg;
                if (awg == null)
                {
                    throw new Exception("AWG not initialized!");
                }

                if (GetInstrument().trx_config == PRIMARY_SOURCE_ANALYZER_CONFIG.VSA_AND_VSG)
                {
                    vsg = GetInstrument().vsg;
                    vsa = GetInstrument().vsa;
                }
                else
                {
                    vsg = GetInstrument().vxt as IVSG;
                    vsa = GetInstrument().vxt as IVSA;
                }

                ss.iqCfFileName = vsa.INIInfo.technology + "_Curr_CFR.wfm";
                ss.etCfFileName = vsa.INIInfo.technology + "_Curr_CFR.csv";
                ss.iqDpdFileName = vsa.INIInfo.technology + "_Curr_DPD.wfm";
                ss.etDpdFileName = vsa.INIInfo.technology + "_Curr_DPD.csv";

                if (PA_Set_DPDET_WaveformParameters.is_flex_sample_rate == true)
                {
                    ss.iqCfFileName = "UserDefined_Curr_CFR.wfm";
                    ss.etCfFileName = "UserDefined_Curr_CFR.csv";
                    ss.iqDpdFileName = "UserDefined_Curr_DPD.wfm";
                    ss.etDpdFileName = "UserDefined_Curr_DPD.csv";
                }

                ss.sigStudioFilePath = GetInstrument().waveform_path;
                //ss.sigStudioWaveformLength = 1.0e-3; // 1ms
                ss.sigStudioTempFilePath = SIG_STUDIO_TEMP_FILE_PATH;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "ET/DPD Init Fail: {0}", ex.Message);
                ret = false;
            }
            return ret;
        }
    }

    [Display(Name: "Set Waveform Parameters", Groups: new string[] { "PA", "ET/DPD" })]
    [Description("Set Waveform Paramters for ET/DPD")]
    public class PA_Set_DPDET_WaveformParameters : PA_EtDpdCommon
    {
        #region Settings
        [Display(Name: "User Defined Over Sampling Rate", Order: 0.1)]
        public bool userdefined_osr { get; set; }

        [Display(Name: "Over Sampling Rate", Order: 1)]
        [EnabledIf("userdefined_osr", true, HideIfDisabled = false)]
        public int over_sampling_rate { get; set; }

        [Display(Name: "Bandwidth", Order: 2)]
        public double bandwidth { get; set; }

        [Display(Name: "Full Waveform", Order: 2.5)]
        public bool fullWaveform { get; set; }

        [Display(Name: "Waveform Length", Order: 3)]
        [EnabledIf("fullWaveform", false, HideIfDisabled = true)]
        public double waveformLength { get; set; }

        private static bool _is_flex_sample_rate;
        [Display(Name: "Is Flexible Sample Rate", Order: 4)]
        public static bool is_flex_sample_rate
        {
            get
            {
                return _is_flex_sample_rate;
            }
            set
            {
                _is_flex_sample_rate = value;
            }
        }

        [Display(Name: "WaveForm Type", Order: 5)]
        [EnabledIf("is_flex_sample_rate", true, HideIfDisabled = true)]
        public WaveformSourceType etdpd_waveform_type { get; set; }

        [Display(Name: "User File Type", Order: 6)]
        [EnabledIf("is_flex_sample_rate", true, HideIfDisabled = true)]
        [EnabledIf("etdpd_waveform_type", WaveformSourceType.UserDefined, HideIfDisabled = true)]
        public UserFileType etdpd_file_type { get; set; }

        [Display(Name: "User File Name", Order: 7)]
        [FilePath]
        [EnabledIf("is_flex_sample_rate", true, HideIfDisabled = true)]
        [EnabledIf("etdpd_waveform_type", WaveformSourceType.UserDefined, HideIfDisabled = true)]
        public string etdpd_file_name { get; set; }

        [Display(Name: "User File Sample Rate (MHz)", Order: 8)]
        [Unit("MHz")]
        [EnabledIf("is_flex_sample_rate", true, HideIfDisabled = true)]
        [EnabledIf("etdpd_waveform_type", WaveformSourceType.UserDefined, HideIfDisabled = true)]
        public double sample_rate_of_flexible { get; set; }

        #endregion

        public PA_Set_DPDET_WaveformParameters()
        {
            over_sampling_rate = 4;
            bandwidth = 5e6;
            waveformLength = 0.001;

            _is_flex_sample_rate = false;
            etdpd_waveform_type = WaveformSourceType.PreDefined;
            etdpd_file_type = UserFileType.TextCsv;
            etdpd_file_name = @"C:\Program Files\Keysight\Power Amplifier Solution\Waveform\UserDefined\ReferenceIQ.txt";
            sample_rate_of_flexible = 5;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Reset();
                sw.Start();

                ret = Init();

                // Step 0. Set SigStudio Waveform Parameter
                if (etdpd_waveform_type == WaveformSourceType.UserDefined)
                {
                    if (File.Exists(etdpd_file_name) == true)
                    {
                        ss.SetWaveformParameters(etdpd_file_name, etdpd_file_type, sample_rate_of_flexible * 1e6, over_sampling_rate, bandwidth);
                        if (fullWaveform)
                        {
                            Logging(LogLevel.WARNING, "Cannot use Full Waveform for User Defined, the waveform length used is {0} second(s).", waveformLength);
                        }
                        ss.sigStudioWaveformLength = waveformLength;
                    }
                    else
                        Logging(LogLevel.ERROR, "No such waveform file: {0}", etdpd_file_name);
                }

                else // Predefined
                {
                    etdpd_file_type = UserFileType.SignalStudio;

                    double bw = vsa.INIInfo.FilterBW;
                    ArbInfo a = new ArbInfo();
                    vsg.GetArbInfo(vsg.SelectedWaveform, ref a);
                    vsg.Arb = a;

                    string file = ss.sigStudioFilePath + "\\" + vsg.SelectedWaveform;

                    if (fullWaveform)
                    {
                        waveformLength = a.numSamples / a.arbSampleRate;
                        ss.sigStudioWaveformLength = waveformLength;
                    }
                    else
                        ss.sigStudioWaveformLength = waveformLength;

                    SUPPORTEDFORMAT format = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology);
                    double bandwidth = GetInstrument().Measurement.TestConditionSetup_GetBandWidth(GetInstrument().selected_technology);
                    int CarrierCount = GetInstrument().Measurement.TestConditionSetup_GetCarrierCount(GetInstrument().selected_technology);


                    // Get bandwidth from INI file
                    if (format.ToString().Contains("WLAN") && Utility.AboutEqual(bandwidth, 20e6))
                    {
                        //used by DPD to upsample 
                        if (!userdefined_osr)
                            over_sampling_rate = 4;
                        //set bandwidth for ACPR calculation, it is not must, because we did not call N7614B to fetch ACPR result
                        bandwidth = 20e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (true, true);
                    }
                    else if (format.ToString().Contains("WLAN") && Utility.AboutEqual(bandwidth, 40e6))
                    {
                        //used by DPD to upsample 
                        if (!userdefined_osr)
                            over_sampling_rate = 2;
                        //set bandwidth for ACPR calculation, it is not must, because we did not call N7614B to fetch ACPR result
                        bandwidth = 40e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (true, true);
                    }
                    //80MHz and 160MHz bandwithis not guarantee in performance by the limit of src&rec's hardware bandwidth
                    else if (format.ToString().Contains("WLAN") && Utility.AboutEqual(bandwidth, 80e6))
                    {
                        //used by DPD to upsample 
                        if (!userdefined_osr)
                            over_sampling_rate = 2;
                        //set bandwidth for ACPR calculation, it is not must, because we did not call N7614B to fetch ACPR result
                        bandwidth = 80e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (true, false);
                    }
                    else if (format.ToString().Contains("WLAN") && Utility.AboutEqual(bandwidth, 160e6))
                    {
                        //used by DPD to upsample 
                        if (!userdefined_osr)
                            over_sampling_rate = 1;
                        //set bandwidth for ACPR calculation, it is not must, because we did not call N7614B to fetch ACPR result
                        bandwidth = 160e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (true, false);
                    }
                    else if (format.ToString().Contains("LTE") && CarrierCount==2)
                    {
                        if (!userdefined_osr)
                            over_sampling_rate = 2;
                        bandwidth = 40e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (false, false);
                    }
                    else if (format.ToString().Contains("5GNR") && (bandwidth > 30.72e6&& bandwidth <= 61.44e6))
                        // BW2 is the range of sample rate that from 30.72MHz to 61.44MHz
                        //The sample rate of waveform created by customer and placed in waveform folder should be 61.44*(3/2) = 92.16MHz, and then
                        //upsampled by factor of 2 times to generate 184.32MHz, that not exceeds the maximum ADC sample rate of VXT
                    {
                        if (!userdefined_osr)
                            over_sampling_rate = 2;
                        bandwidth = 61.44e6;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (false, false);
                    }
                    else
                    {
                        if (!userdefined_osr)
                            over_sampling_rate = 4;
                        awg.SetClockFrequencyAndIdealDelay(format, bandwidth);// (false, false);
                    }

                    if (File.Exists(file) == true)
                        ss.SetWaveformParameters(file, UserFileType.SignalStudio, a.arbSampleRate,
                            over_sampling_rate, bandwidth);
                    else
                        Logging(LogLevel.ERROR, "No such waveform file: {0}", file);
                }

                GetInstrument().etdpd_bandwidth = bandwidth;
                GetInstrument().etdpd_over_sample_rate = over_sampling_rate;

                GetInstrument().is_flexible_samplerate = is_flex_sample_rate;
                GetInstrument().etdpd_waveform_type = etdpd_waveform_type;
                GetInstrument().etdpd_file_type = etdpd_file_type;
                GetInstrument().etdpd_file_name = etdpd_file_name;
                GetInstrument().etdpd_sample_rate = sample_rate_of_flexible;

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Set ET/DPD Waveform Parameter Fail: {0}", ex.Message);
                ret = false;
            }

            PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Set ET/DPD Waveform Parameter Done, Time:{3,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Set CFR Parameters", Groups: new string[] { "PA", "ET/DPD" })]
    [Description("Set Crest Factor Reduction Paramters")]
    public class PA_SetCFRParameters : PA_EtDpdCommon
    {
        #region Settings
        [Display(Name: "Enable CFR", Order: 10)]
        public bool cfr_enabled { get; set; }

        [Display(Name: "Target PAPR", Order: 11)]
        [EnabledIf("cfr_enabled", true, HideIfDisabled = true)]
        public double cfr_target_papr { get; set; }

        [Display(Name: "CFR Algorithm", Order: 11)]
        [EnabledIf("cfr_enabled", true, HideIfDisabled = true)]
        public CfrAlgorithm cfr_algorithm { get; set; }
        #endregion

        public PA_SetCFRParameters()
        {
            cfr_enabled = false;
            cfr_target_papr = 0.0;
            cfr_algorithm = CfrAlgorithm.PEAK_WINDOWING;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Reset();
                sw.Start();

                ret = Init();

                if (ss != null)
                {
                    ss.SetCfrParameters(cfr_enabled, cfr_target_papr, cfr_algorithm == CfrAlgorithm.PEAK_WINDOWING ? true : false);
                }
                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Apply CFR Fail: {0}", ex.Message);
                ret = false;
            }

            PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Set CFR Parameter Done, Time:{3,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Set ET Parameters", Groups: new string[] { "PA", "ET/DPD" })]
    [Description("Set Envelop Tracking Parameters")]
    public class PA_SetETParameters : PA_EtDpdCommon
    {
        #region Settings
        [Display(Name: "Enable ET", Order: 200)]
        public bool et_enabled { get; set; }

        [Display(Name: "Envelop Input Type", Group: "ET", Order: 201)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        [Browsable(false)]
        public EnvInputType et_env_input_type { get; set; }

        [Display(Name: "Envelop Override Absolute RF Amplitude", Group: "ET", Order: 202)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        [Browsable(false)]
        public double et_env_arb_rf_over { get; set; }

        [Display(Name: "Shaping Table File", Group: "ET", Order: 203)]
        [FilePath]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        public string et_shaping_table_file { get; set; }

        [Display(Name: "ET Arb Model", Group: "ET", Order: 204)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        public string et_arb_model { get; set; }

        private bool _is_flex_sample_rate;
        [Display(Name: "Is Flexible Sample Rate", Group: "ET", Order: 205)]
        public bool is_flex_sample_rate
        {
            get
            {
                return _is_flex_sample_rate;
            }
            set
            {
                _is_flex_sample_rate = value;
            }
        }

        private int _osr_for_envelope;
        [Display(Name: "OSR for Envelope", Group: "ET", Order: 206)]
        [EnabledIf("is_flex_sample_rate", true, HideIfDisabled = true)]
        public int osr_for_envelope
        {
            get { return _osr_for_envelope; }
            set
            {
                _osr_for_envelope = value;
                if (_osr_for_envelope >= 32)
                {
                    _osr_for_envelope = 32;
                }
            }
        }

        [Display(Name: "ETPS V_Cm", Group: "ETPS", Order: 220)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        public double etps_vcm { get; set; }

        [Display(Name: "ETPS V_Ref", Group: "ETPS", Order: 221)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        public double etps_vref { get; set; }

        [Display(Name: "ETPS Gain", Group: "ETPS", Order: 222)]
        [EnabledIf("et_enabled", true, HideIfDisabled = true)]
        [Unit("dB")]
        public double etps_gain { get; set; }
        #endregion

        public PA_SetETParameters()
        {
            et_enabled = false;
            et_env_input_type = EnvInputType.NORMALIZED_IQ;
            et_env_arb_rf_over = 0;
            et_shaping_table_file = null;
            et_arb_model = "Signadyne";
            etps_gain = 13.0;
            etps_vcm = 0.4;
            etps_vref = 2.75;

            is_flex_sample_rate = true;
            osr_for_envelope = 5;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Reset();
                sw.Start();

                ret = Init();

                if (et_enabled)
                {
                    if (!File.Exists(et_shaping_table_file))
                    {
                        throw new Exception("No ET shaping table file!");
                    }

                    //change dB value to linear value
                    double etps_gain_lin = Math.Pow(10, etps_gain / 20) / 2.0;

                    bool isWLAN = GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology).ToString().Contains("WLAN");

                    if (is_flex_sample_rate)
                    {
                        double dIntermediateEnvSampleRate = GetInstrument().etdpd_sample_rate * GetInstrument().etdpd_over_sample_rate * 1e6 * osr_for_envelope;
                        ss.SetETParameters(isWLAN, et_enabled, dIntermediateEnvSampleRate, et_env_input_type, et_env_arb_rf_over, et_shaping_table_file,
                                            et_arb_model, GetInstrument().etdpd_waveform_type, GetInstrument().etdpd_file_type,
                                            GetInstrument().etdpd_sample_rate * 1e6, etps_vcm, etps_vref, etps_gain_lin, is_flex_sample_rate);

                        awg.SetClockFrequencyForFlexSampleRate(GetInstrument().etdpd_sample_rate * GetInstrument().etdpd_over_sample_rate * 1e6, osr_for_envelope * ss.fixedOsr);
                    }
                    else
                    {
                        ss.SetETParameters(isWLAN, et_enabled, awg.ClockFrequency, et_env_input_type, et_env_arb_rf_over, et_shaping_table_file,
                            et_arb_model, GetInstrument().etdpd_waveform_type, GetInstrument().etdpd_file_type,
                            GetInstrument().etdpd_sample_rate * 1e6, etps_vcm, etps_vref, etps_gain_lin, is_flex_sample_rate);
                    }

                    awg.channelOffset = etps_vcm;
                }
                else
                {
                    ss.etEnabled = false;
                }

                GetInstrument().et_enabled = et_enabled;
                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.WARNING, "Make sure Set ET/DPD Waveform Parameter before Set ET Parameter");
                Logging(LogLevel.ERROR, "Set ET Parameters Fail: {0}", ex.Message);
                ret = false;
            }

            PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Set ET Parameter Done, Time:{3,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Set DPD Parameters", Groups: new string[] { "PA", "ET/DPD" })]
    [Description("Set Digital Pre-Distortion Parameters")]
    public class PA_SetDPDParameters : PA_EtDpdCommon
    {
        public enum MomoryModelDefinition
        {
            LSE_USING_QR = 0,
            LSE_USING_SVD = 1
        }

        #region Settings
        [Display(Name: "DPD Enabled", Group: "DPD", Order: 1)]
        public bool dpd_enabled { get; set; }

        [Display(Name: "Model Type", Group: "DPD", Order: 10)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        public DpdModelType dpd_model_type { get; set; }

        [Display(Name: "Waveform Length", Group: "DPD", Order: 11)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        public double dpd_waveform_length { get; set; }

        [Display(Name: "Analysis Delay", Group: "DPD", Order: 12)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        public double dpd_analysis_delay { get; set; }

        [Display(Name: "LUT Size", Group: "DPD", Order: 13)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.LUT, HideIfDisabled = true)]
        public int dpd_lut_size { get; set; }

        [Display(Name: "AmAm Polynomial", Group: "DPD", Order: 14)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.LUT, HideIfDisabled = true)]
        public int dpd_amam_polynomial { get; set; }

        [Display(Name: "AmPm Polynomial", Group: "DPD", Order: 15)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.LUT, HideIfDisabled = true)]
        public int dpd_ampm_polynomial { get; set; }

        [Display(Name: "Model Identification Algorithm", Group: "DPD", Order: 16)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.MEMORY, HideIfDisabled = true)]
        public MomoryModelDefinition dpd_model_id { get; set; }

        [Display(Name: "Memory Order", Group: "DPD", Order: 17)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.MEMORY, DpdModelType.VOLTERA, HideIfDisabled = true)]
        public int dpd_memory_order { get; set; }

        [Display(Name: "Non-linear Order", Group: "DPD", Order: 18)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.MEMORY, DpdModelType.VOLTERA, HideIfDisabled = true)]
        public int dpd_nonlinear_order { get; set; }

        [Display(Name: "Cross Term Order", Group: "DPD", Order: 19)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.VOLTERA, HideIfDisabled = true)]
        public int dpd_cross_term_order { get; set; }

        [Display(Name: "Odd Orders Only", Group: "DPD", Order: 20)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.MEMORY, DpdModelType.VOLTERA, HideIfDisabled = true)]
        public bool dpd_odd_orders_only { get; set; }

        [Display(Name: "User Defined DLL File", Group: "DPD", Order: 30)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.USER_DEFINED, HideIfDisabled = true)]
        [FilePath]
        public string user_defined_dll_file { get; set; }

        [Display(Name: "User Defined Class Name", Group: "DPD", Order: 30.5)]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.USER_DEFINED, HideIfDisabled = true)]
        public string user_defined_classname { get; set; }

        [Display(Name: "User Defined DPD Model Extraction Function", Group: "DPD", Order: 31,
            Description: "<FuncName> (\n" +
                         "     Complex[] reference,\n" +
                         "     Complex[] measured,\n" +
                         " out Complex[] predistorted,\n" +
                         " ref Complex[] lut_or_coef);\n")]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.USER_DEFINED, HideIfDisabled = true)]
        public string user_defined_model_extract_func { get; set; }

        [Display(Name: "User Defined Get AmAm/AmPm Data Function", Group: "DPD", Order: 32,
            Description: "<FuncName> (\n" +
                         "     Complex[] reference,\n" +
                         "     Complex[] measured,\n" +
                         " ref Array am_X,\n" +
                         " ref Array am_Y,\n" +
                         " ref Array pm_Y);\n")]
        [EnabledIf("dpd_enabled", true, HideIfDisabled = true)]
        [EnabledIf("dpd_model_type", DpdModelType.USER_DEFINED, HideIfDisabled = true)]
        public string user_defined_get_amam_ampm_data_func { get; set; }

        #endregion

        public PA_SetDPDParameters()
        {
            dpd_enabled = false;
            dpd_model_type = DpdModelType.LUT;
            dpd_lut_size = 128;
            dpd_amam_polynomial = 3;
            dpd_ampm_polynomial = 3;
            dpd_model_id = MomoryModelDefinition.LSE_USING_QR;
            dpd_memory_order = 1;
            dpd_nonlinear_order = 5;
            dpd_cross_term_order = 5;
            dpd_odd_orders_only = false;
            dpd_waveform_length = 1.0e-4;
            dpd_analysis_delay = 0;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Reset();
                sw.Start();

                ret = Init();

                if (dpd_enabled)
                {
                    ss.SetDpdParameters(dpd_enabled, WaveformSourceType.PreDefined, dpd_model_type, UserDefinedModelType.Matlab_Script, dpd_waveform_length, dpd_analysis_delay, dpd_lut_size,
                        dpd_amam_polynomial, dpd_ampm_polynomial, dpd_model_id == MomoryModelDefinition.LSE_USING_QR ? true : false,
                        dpd_memory_order, dpd_nonlinear_order, dpd_cross_term_order, dpd_odd_orders_only);
                    if (dpd_enabled && dpd_model_type == DpdModelType.USER_DEFINED)
                    {
                        ud_dpd.user_defined_dll_file = user_defined_dll_file;
                        ud_dpd.user_defined_dll_classname = user_defined_classname;
                        ud_dpd.user_defined_extract_dpd_model_func = user_defined_model_extract_func;
                        ud_dpd.user_defined_get_amam_ampm_func = user_defined_get_amam_ampm_data_func;
                    }
                }
                else
                {
                    ss.dpdEnabled = false;
                }

                GetInstrument().dpd_enabled = dpd_enabled;
                sw.Stop();
            }

            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Apply DPD Fail: {0}", ex.Message);
                ret = false;
            }

            PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Set DPD Parameter Done, Time:{3,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }
    }

    [Display(Name: "Apply ET/DPD", Groups: new string[] { "PA", "ET/DPD" })]
    [Description("Apply ET/DPD")]
    public class PA_ApplyEt : PA_EtDpdCommon
    {
        public enum ET_FIXED_DELAY_CAL_METHOD
        {
            ET_FIXED_DELAY_CAL_AUTO,
            ET_FIXED_DELAY_CAL_MANUAL
        }

        public enum ET_TRIGGER_DIR
        {
            VSG_TO_AWG,
            AWG_TO_VSG
        }

        #region Settings
        private bool _do_powerservo;
        [Display(Name: "Measure PowerServo", Group: "Power Servo", Order: 10)]
        public bool do_powerservo
        {
            get { return _do_powerservo; }
            set
            {
                _do_powerservo = value;
                meas_pout = _do_powerservo;
            }
        }

        [Display(Name: "PA Gain", Group: "Power Servo", Order: 11)]
        [Unit("dB")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_gain { get; set; }

        [Display(Name: "Target PA Output", Group: "Power Servo", Order: 12)]
        [Unit("dBm")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public double target_pa_output { get; set; }

        [Display(Name: "Maximum Loop Count", Group: "Power Servo", Order: 14)]
        [Unit("time(s)")]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public int max_loop_count { get; set; }

        [Display(Name: "Use FFT for Power Servo", Group: "Power Servo", Order: 15)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool fft_servo { get; set; }

        #region POut
        [Display(Name: "Measure POut", Group: "Test Items", Order: 100)]
        [EnabledIf("do_powerservo", true, HideIfDisabled = true)]
        public bool meas_pout { get; set; }

        [Display(Name: "POut High Limit", Group: "Test Items", Order: 101)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_high_limit { get; set; }

        [Display(Name: "POut Low Limit", Group: "Test Items", Order: 102)]
        [Unit("dB")]
        [EnabledIf("meas_pout", true, HideIfDisabled = true)]
        public double pout_low_limit { get; set; }
        #endregion

        #region ET Parameters
        [Display(Name: "Apply ET", Group: "ET", Order: 200.001)]
        public bool apply_et { get; set; }

        [Display(Name: "ET Trigger Direction", Group: "ET", Order: 200.01)]
        [EnabledIf("apply_et", true, HideIfDisabled = true)]
        public ET_TRIGGER_DIR et_trig_dir { get; set; }

        [Display(Name: "ET VSG->AWG Delay Cal Method",
            Description: "Auto: The Calibrated VSG/VXT->AWG delay value will be used with search window +/-250ns\n"
                       + "Manual: The User Input VSG/VXT->AWG delay value will be used with search window +/-<Manual Range>",
            Group: "ET", Order: 200.011)]
        [EnabledIf("apply_et", true, HideIfDisabled = true)]
        [EnabledIf("et_trig_dir", ET_TRIGGER_DIR.VSG_TO_AWG, HideIfDisabled = true)]
        [Browsable(true)]
        public ET_FIXED_DELAY_CAL_METHOD et_fixed_delay_cal_method { get; set; }

        [Display(Name: "ET Manual VSG->AWG Delay", Group: "ET", Order: 200.012,
            Description: "User defined delay from the time VSG generate trigger to the time AWG generate corresponding envelope signal")]
        [EnabledIf("apply_et", true, HideIfDisabled = true)]
        [EnabledIf("et_trig_dir", ET_TRIGGER_DIR.VSG_TO_AWG, HideIfDisabled = true)]
        [EnabledIf("et_fixed_delay_cal_method", ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_MANUAL, HideIfDisabled = true)]
        [Browsable(true)]
        [Unit("ns")]
        public int manual_delay { get; set; }

        [Display(Name: "ET Manual Fine Adjust Window +/-", Group: "ET", Order: 200.013,
            Description: "User specified ET Manual fine adjust window to make the RF and envelope signal aligned")]
        [EnabledIf("apply_et", true, HideIfDisabled = true)]
        [EnabledIf("et_trig_dir", ET_TRIGGER_DIR.VSG_TO_AWG, HideIfDisabled = true)]
        [EnabledIf("et_fixed_delay_cal_method", ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_MANUAL, HideIfDisabled = true)]
        [Browsable(true)]
        [Unit("ns")]
        public int manual_search_window { get; set; }

        private bool _et_generate_acpr_results;
        [Display(Name: "Generate ET Align ACPR Results", Group: "ET", Order: 200.02)]
        [EnabledIf("apply_et", true, HideIfDisabled = true)]
        public bool et_generate_acpr_results
        {
            get { return _et_generate_acpr_results; }
            set
            {
                _et_generate_acpr_results = value;
                //GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.ET)].EnableRealTime
                //    = _et_generate_acpr_results;
            }
        }
        #endregion

        #region DPD Parameters
        [Display(Name: "Apply DPD", Group: "DPD", Order: 300.001)]
        public bool apply_dpd { get; set; }

        private bool _generate_amam_ampm;
        [Display(Name: "Generate AM/AM and AM/PM Data", Group: "DPD", Order: 301)]
        [EnabledIf("apply_dpd", true, HideIfDisabled = true)]
        public bool generate_amam_ampm
        {
            get { return _generate_amam_ampm; }
            set
            {
                _generate_amam_ampm = value;
                //GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.DPD)].EnableRealTime
                //    = _generate_amam_ampm;
            }
        }
        #endregion

        #endregion

        public PA_ApplyEt()
        {
            target_pa_gain = 15;
            target_pa_output = -20;
            max_loop_count = 10;
            fft_servo = true;
            pout_high_limit = 0.1;
            pout_low_limit = -0.1;

            apply_et = false;
            et_trig_dir = ET_TRIGGER_DIR.VSG_TO_AWG;
            et_fixed_delay_cal_method = ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_AUTO;

            apply_dpd = false;
            generate_amam_ampm = false;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
        }

        public override void Run()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            bool ret1 = true;
            bool ret2 = true;
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Reset();
                sw.Start();

                if (do_powerservo)
                {
                    GetInstrument().applied_target_pout = target_pa_output;
                }

                if (apply_dpd || apply_et)
                {
                    ret = Init();
                    if (!ret)
                        throw new Exception("Init Error!");

                    // Step 1. Generate RF Waveform, Please NOTE, must reset VSG Power after PlayARB
                    ss.SetPAParameters(target_pa_output, target_pa_gain);

                    // Only Re-Generate RF waveform when either waveform changed or reference not generated yet
                    if (GetInstrument().waveform_changed || !GetInstrument().reference_generated)
                    {
                        ss.GenerateRFWaveform();
                        GetInstrument().reference_generated = true;
                    }

                    vsg.PlayARB(ss.iqCfFileName, ss.sigStudioWaveformLength, true);
                }

                if (apply_et)
                {
                    ret1 = ApplyEt();
                    if (!ret1)
                        throw new Exception("ApplyEt() Error!");
                    TestPlanRunningHelper.GetInstance().IsEtActive = ret1;
                }

                if (apply_dpd)
                {
                    ret2 = ApplyDpd();
                    if (!ret2)
                        throw new Exception("ApplyDpd() Error!");
                    TestPlanRunningHelper.GetInstance().IsDPDActive = ret1;
                }
                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Apply ET/DPD Fail: {0}", ex.Message);
                ret = false;
            }

            ret = ret && ret1 && ret2;

            PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, Apply ET {3}/ Apply DPD {4}, Time:{5,7:F2}ms",
                  ret ? "Pass" : "Fail",
                  TechnologyName(GetInstrument().selected_technology),
                  GetInstrument().selected_frequency,
                  ret1 ? "Pass" : "Fail",
                  ret2 ? "Pass" : "Fail",
                  (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

            UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

            // If no verdict is used, the verdict will default to NotSet.
            // You can change the verdict using UpgradeVerdict() as shown below.
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
            // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
        }



        private bool ApplyDpd()
        {
            bool ret = true;
            try
            {
                if (GetInstrument().dpd_enabled && apply_dpd)
                {
                    Array am_x = null;
                    Array am_y = null;
                    Array pm_y = null;
                    Array post_am_x = null;
                    Array post_am_y = null;
                    Array post_pm_y = null;

                    ss.useDpdForEnv = GetInstrument().dpd_enabled && GetInstrument().et_enabled
                                      && apply_et && apply_dpd;

                    object measured = null;
                    object lut_or_coef = null;
                    object predistorted = null;

                    meas.TestConditionSetup_Trigger(false, true,
                        (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                        (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                        100e-6, AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal,
                        GetInstrument().receiver_trig_delay,
                        AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
                        100,
                        (AnalyzerTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                        true, true, true);

                    // Extract Am/Am Am/Pm Data before doing DPD
                    ss.CreateMeasuredData(ss.dpdEvalTime, true, ref measured);
                    ss.ExtractDpdModel(ss.reference, measured, out predistorted, ref lut_or_coef, false);
                    GenerateDpdResult("Pre-DPD", ref am_x, ref am_y, ref pm_y);

                    double[] prePower = null;
                    double[] postPower = null;
                    bool bOverloaded = false;
                    double fStart = 0;
                    double fStep = 0;
                    double cableLoss = Utility.getOutputAtten(GetInstrument().selected_frequency);

                    if (GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.DPD)].EnableRealTime)
                    {
                        vsa.GetPowerSpectrum(ref prePower, ref bOverloaded, ref fStart, ref fStep);
                        for (int i = 0; i < prePower.Length; i++)
                        {
                            prePower[i] += cableLoss;
                        }

                        Dictionary<string, object> predpdDic = new Dictionary<string, object>();
                        Keysight.CommunicationsFabric.Protocol.ArgMap pre_dpd_map = new Keysight.CommunicationsFabric.Protocol.ArgMap();
                        pre_dpd_map["AMX"] = am_x;
                        pre_dpd_map["AMY"] = am_y;
                        pre_dpd_map["PMY"] = pm_y;
                        pre_dpd_map["ACPR"] = prePower;

                        predpdDic[MeasurementDataStructure.Value] = pre_dpd_map;
                        predpdDic[MeasurementDataStructure.IsSubMeas] = true;
                        predpdDic[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                        predpdDic[MeasurementDataStructure.SubMeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.DPD_PRE);
                        predpdDic[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                        MeasurementDataCommunicationHelper.PublishRealTimeResults(predpdDic);
                    }

                    // Apply the DPD Model to the waveforms
                    if (predistorted != null)
                        ss.ApplyDpdModel(predistorted);

                    if (ss.etEnabled && apply_et)
                    {

                        if (et_trig_dir == ET_TRIGGER_DIR.VSG_TO_AWG)
                        {
                            // Start the Envelope waveform
                            vsg.StopModulation();
                            vsg.SetupTrigger(false, true, syncOutputTriggerDest: (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2), apply: false);

                            if (ss.useDpdForEnv)
                                awg.PlayWaveform(ss.etDpdFileName, true);
                            else
                                awg.PlayWaveform(ss.etCfFileName, true);

                            vsg.PlayARB(ss.iqDpdFileName, ss.sigStudioWaveformLength, true);

                            // Wait 1 waveform cycle or a minimum of 5 ms
                            System.Threading.Thread.Sleep((int)(Math.Max(5, ss.sigStudioWaveformLength * 1000 + 1)));
                        }

                        else if (et_trig_dir == ET_TRIGGER_DIR.AWG_TO_VSG)
                        {
                            vsg.StopModulation();
                            vsg.SetupTrigger(true, false, extTrigSource: (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2), apply: false);
                            vsg.PlayByTriggerEvent(ss.iqDpdFileName, (int)GetInstrument().chassis_trigger_line + 2, 0.0, true);

                            if (ss.useDpdForEnv)
                                awg.PlayWaveform(ss.etDpdFileName, false);
                            else
                                awg.PlayWaveform(ss.etCfFileName, false);
                        }
                        else
                        {
                            throw new Exception("Unknown ET trigger type!");
                        }

                        // Wait 1 waveform cycle or a minimum of 5 ms
                        System.Threading.Thread.Sleep((int)(Math.Max(5, ss.sigStudioWaveformLength * 1000 + 1)));
                    }
                    else
                    {
                        vsg.PlayARB(ss.iqDpdFileName, ss.sigStudioWaveformLength, true);
                    }

                    // Do a PowerServo to adjust the power to the expected value
                    if (do_powerservo)
                    {
                        Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                        tps.Add(TestItem_Enum.POUT, new TestPoint("", target_pa_output + pout_low_limit, target_pa_output + pout_high_limit));
                        GetInstrument().Measurement.ServoInputPower(vsg.Frequency, target_pa_output, target_pa_gain, fft_servo, max_loop_count, tps);
                    }

                    // Extract Am/Am Am/Pm Data after doing DPD
                    if (generate_amam_ampm)
                    {
                        ss.CreateMeasuredData(ss.dpdEvalTime, true, ref measured);
                        ss.ExtractDpdModel(ss.reference, measured, out predistorted, ref lut_or_coef, false);
                        GenerateDpdResult("Post-DPD", ref post_am_x, ref post_am_y, ref post_pm_y);
                    }

                    if (GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.DPD)].EnableRealTime)
                    {
                        vsa.GetPowerSpectrum(ref postPower, ref bOverloaded, ref fStart, ref fStep);

                        for (int i = 0; i < postPower.Length; i++)
                        {
                            postPower[i] += cableLoss;
                        }

                        Dictionary<string, object> postdpdDic = new Dictionary<string, object>();
                        Keysight.CommunicationsFabric.Protocol.ArgMap post_dpd_map = new Keysight.CommunicationsFabric.Protocol.ArgMap();
                        post_dpd_map["AMX"] = post_am_x;
                        post_dpd_map["AMY"] = post_am_y;
                        post_dpd_map["PMY"] = post_pm_y;
                        post_dpd_map["ACPR"] = postPower;

                        postdpdDic[MeasurementDataStructure.Value] = post_dpd_map;
                        postdpdDic[MeasurementDataStructure.IsSubMeas] = true;
                        postdpdDic[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                        postdpdDic[MeasurementDataStructure.SubMeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.DPD_POST);
                        postdpdDic[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                        MeasurementDataCommunicationHelper.PublishRealTimeResults(postdpdDic);
                    }
                    //     PublishDPDResults("Post-DPD", am_x, am_y, pm_y, postPower);



                }
                else
                {
                    Logging(LogLevel.WARNING, "Apply DPD is called but DPD is not enabled!");
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Apply DPD Fail: {0}", ex.Message);
            }
            return ret;
        }

        private bool ApplyEt()
        {
            // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
            // ToDo: Add test case code here
            bool ret = true;
            double iqDelay = 200e-9;
            try
            {
                ret = Init();

                if (GetInstrument().et_enabled && apply_et)
                {
                    // Step 2. Generate Envelope
                    awg.ClearMemory();
                    ss.GenerateEnvelope(ss.reference);

                    // Step 3. Play Waveform
                    if (et_trig_dir == ET_TRIGGER_DIR.VSG_TO_AWG)
                    {
                        // If Manual Delay is used, update the IdealSampleDelay variable in AWG
                        if (et_fixed_delay_cal_method == ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_MANUAL)
                        {
                            awg.UpdateFixedDelay(ss.etCfFileName, ss.etSampleRate, manual_delay * 1e-9);
                        }

                        // Setup AWG and then Play wavefrom from VSG
                        vsg.StopModulation();
                        vsg.SetupTrigger(false, true, syncOutputTriggerDest: (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2), apply: false);
                        awg.SetTriggerIn((int)GetInstrument().chassis_trigger_line, 0);
                        awg.PlayWaveform(ss.etCfFileName, true);

                        vsg.SetIQDelay(iqDelay, false);
                        vsg.PlayARB(ss.iqCfFileName, ss.sigStudioWaveformLength, true);

                        vsa.SetupTrigger(AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal,
                            GetInstrument().receiver_trig_delay,
                            AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
                            100,
                            (AnalyzerTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                            true);
                    }
                    else if ((GetInstrument().is_flexible_samplerate) && (et_trig_dir == ET_TRIGGER_DIR.AWG_TO_VSG))
                    {
                        // Setup VSG and then Play wavefrom from AWG
                        vsg.StopModulation();
                        vsg.SetupTrigger(true, false, extTrigSource: (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2), apply: false);
                        awg.SetTriggerOut((int)GetInstrument().chassis_trigger_line, 1);

                        vsg.PlayByTriggerEvent(ss.iqCfFileName, (int)GetInstrument().chassis_trigger_line + 2, 0.0, true);

                        vsa.SetupTrigger(AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal,
                            GetInstrument().receiver_trig_delay,
                            AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
                            100,
                            (AnalyzerTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                            true);

                        //calculate delay value for AWG and VSG
                        string strToLookUp = (awg.ClockFrequency / 1e6).ToString(".0000");
                        double AWGDelay = GetInstrument().iniReadForFlexSampleRate.IniReadDoubleValue("data", strToLookUp);

                        int nAWGDelay = 0;
                        double dFreq = awg.ClockFrequency;
                        if ((dFreq < 400e6) | (dFreq > 1e9))
                        {
                            Logging(LogLevel.ERROR, "Not a valid Frequency value for AWG."); // out of range
                        }

                        int delayMultiplier = 10;
                        int delayConstant = 16;
                        double VXTSyncOffsetOfThisPowerOn = vsg.GetSyncOffsetOfThisPowerOn();
                        AWGDelay = AWGDelay - GetInstrument().VXTSyncOffsetOfCalibration * 1e9 + VXTSyncOffsetOfThisPowerOn * 1e9;
                        nAWGDelay = (int)(AWGDelay * 1e-9 / (1 / dFreq * delayMultiplier));
                        double BBDelay = (nAWGDelay * delayMultiplier / dFreq) * 1e9 - AWGDelay;
                        BBDelay -= delayConstant;

                        BBDelay = BBDelay * 1e-9;//convert to Nanosecond

                        // Configure AWG trigger, play waveform with trigger
                        // AWG 0, markermode start, trgPXImask (PXI 2), IOmask 0, value 1 (PXI uses negative logic), syncmode SYNC100, 
                        // lenght (20x5xTclk)ns, delayPostWaveform (0x5xTclk)ns (delay preWaveform is configured by delaying the waveform start)
                        int nAWG = 0;           // channel 0
                                                //  markerMode: 0=>Disabled, 1=>On WF Start, 2=>On WF Start after WF Delay, 3=>ON every Cycle, 4=>End (not implemented)
                        int markerMode = 3;     // Marker referenced to start of waveform ignoring the WF delay set in AWGqueuewaveform
                        int PXImask = 1 << ((int)GetInstrument().chassis_trigger_line);   // PXItrg N
                        int IOmask = 1;     // front-panel IOtrg output enabled
                        int TrgValue = 0;   // 1=>PulseActive : IOtrg=1, PXItrg=0 - 0=>PulseNotActive: IOtrg=0, PXItrg=1
                        int trgSync = 1;    // 1=>Sync with CLK10
                        int trgWidth = 18;  // width = 18x5xTclk
                        int trgDelay = 0;   // delay = 0x5xTclk
                        try
                        {
                            awg.SetupMarker(nAWG, markerMode, PXImask, IOmask, TrgValue, trgSync, trgWidth, trgDelay);
                            awg.PlayWaveformForFlexSampleRate(ss.etCfFileName, false, nAWGDelay);
                        }
                        catch (Exception ex)
                        {
                            Logging(LogLevel.ERROR, "AWG play error: " + ex.Message);
                        }

                        vsg.SetIQDelay(BBDelay, true);

                    }
                    else if (et_trig_dir == ET_TRIGGER_DIR.AWG_TO_VSG)
                    {
                        // Setup VSG and then Play wavefrom from AWG
                        vsg.StopModulation();
                        vsg.SetupTrigger(true, false, extTrigSource: (SourceTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2), apply: false);
                        awg.SetTriggerOut((int)GetInstrument().chassis_trigger_line, 1);

                        vsg.PlayByTriggerEvent(ss.iqCfFileName, (int)GetInstrument().chassis_trigger_line + 2, 0.0, true);

                        vsa.SetupTrigger(AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal,
                            GetInstrument().receiver_trig_delay,
                            AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
                            100,
                            (AnalyzerTriggerEnum)((int)GetInstrument().chassis_trigger_line + 2),
                            true);

                        // Configure AWG trigger, play waveform with trigger
                        // AWG 0, markermode start, trgPXImask (PXI 2), IOmask 0, value 1 (PXI uses negative logic), syncmode SYNC100, 
                        // lenght (20x5xTclk)ns, delayPostWaveform (0x5xTclk)ns (delay preWaveform is configured by delaying the waveform start)
                        int nAWG = 0;           // channel 0
                                                //  markerMode: 0=>Disabled, 1=>On WF Start, 2=>On WF Start after WF Delay, 3=>ON every Cycle, 4=>End (not implemented)
                        int markerMode = 3;     // Marker referenced to start of waveform ignoring the WF delay set in AWGqueuewaveform
                        int PXImask = 1 << ((int)GetInstrument().chassis_trigger_line);   // PXItrg N
                        int IOmask = 1;     // front-panel IOtrg output enabled
                        int TrgValue = 0;   // 1=>PulseActive : IOtrg=1, PXItrg=0 - 0=>PulseNotActive: IOtrg=0, PXItrg=1
                        int trgSync = 1;    // 1=>Sync with CLK10
                        int trgWidth = 18;  // width = 18x5xTclk
                        int trgDelay = 0;   // delay = 0x5xTclk
                        try
                        {
                            awg.SetupMarker(nAWG, markerMode, PXImask, IOmask, TrgValue, trgSync, trgWidth, trgDelay);
                            awg.PlayWaveform(ss.etCfFileName, false);
                        }
                        catch (Exception ex)
                        {
                            Logging(LogLevel.ERROR, "AWG play error: " + ex.Message);
                        }

                    }
                    else
                    {
                        throw new Exception("Not supported ET Trigger Type!");
                    }

                    // Wait 1 waveform cycle or a minimum of 5 ms
                    System.Threading.Thread.Sleep((int)(Math.Max(5, ss.sigStudioWaveformLength * 1000 + 1)));

                    ACPR_FORMAT_TYPE t;
                    if (GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.GSM ||
                        GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology) == SUPPORTEDFORMAT.EDGE)
                    {
                        t = ACPR_FORMAT_TYPE.ACPR_GSM_FORMAT;
                    }
                    else if (GetInstrument().Measurement.TestConditionSetup_GetFormat(GetInstrument().selected_technology).ToString().Contains("LTE"))
                    {
                        t = ACPR_FORMAT_TYPE.ACPR_LTE_FORMAT;
                    }
                    else
                    {
                        t = ACPR_FORMAT_TYPE.ACPR_STANDARD_FORMAT;
                    }

                    if (GetInstrument().is_flexible_samplerate == false)
                    {
                        // Do ET Align
                        Stopwatch sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        string et_align_mode = "Default";
                        if (et_fixed_delay_cal_method == ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_AUTO)
                        {
                            iqDelay = ss.EtAlign(et_align_mode,
                                               t,
                                               do_powerservo,
                                               vsa.Frequency,
                                               target_pa_output,
                                               target_pa_gain,
                                               Math.Min(Math.Abs(pout_high_limit), Math.Abs(pout_low_limit)),
                                               true, max_loop_count);
                            vsg.SetIQDelay(iqDelay, true);
                        }
                        else if (et_fixed_delay_cal_method == ET_FIXED_DELAY_CAL_METHOD.ET_FIXED_DELAY_CAL_MANUAL)
                        {
                            if (manual_search_window > 0 && manual_search_window <= 250)
                            {
                                iqDelay = ss.EtAlign(et_align_mode,
                                                   t,
                                                   do_powerservo,
                                                   vsa.Frequency,
                                                   target_pa_output,
                                                   target_pa_gain,
                                                   Math.Min(Math.Abs(pout_high_limit), Math.Abs(pout_low_limit)),
                                                   true, max_loop_count,
                                                   manual_search_window * -1.0e-9,
                                                   manual_search_window * 1.0e-9);
                            }
                            else
                            {
                                if (manual_search_window > 250)
                                {
                                    Logging(LogLevel.WARNING, "Invalid ET Manual Search Window, Should be in (0, 250)ns range. Use 0ns Search Window");
                                }
                                iqDelay = 0;
                            }
                            vsg.SetIQDelay(iqDelay, true);
                        }
                        sw.Stop();

                        // Do PowerServo here if not doing DPD
                        if (do_powerservo && !apply_dpd)
                        {
                            Dictionary<TestItem_Enum, TestPoint> tps = new Dictionary<TestItem_Enum, TestPoint>();
                            tps.Add(TestItem_Enum.POUT, new TestPoint("", target_pa_output + pout_low_limit, target_pa_output + pout_high_limit));
                            GetInstrument().Measurement.ServoInputPower(vsg.Frequency, target_pa_output, target_pa_gain, fft_servo, max_loop_count, tps);
                        }

                        PA_LOG(PA_LOG_TRS, "DPDET", "[{0}] {1,12}, Freq:{2,5:F0}MHz, ET Align Calculated IQ Delay is {3,9:F4}ns, Time:{4,7:F2}ms",
                              ret ? "Pass" : "Fail",
                              TechnologyName(GetInstrument().selected_technology),
                              GetInstrument().selected_frequency,
                              (iqDelay + awg.IdealSampleDelay) * 1e9,
                              (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000.00);

                        if (et_generate_acpr_results)
                        {
                            // Log IQDelay->ACPR array
                            object x = null;
                            object y1 = null;
                            object y2 = null;
                            ss.GetAlignData1(ref x, ref y1, ref y2);
                            if (GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.ET)].EnableRealTime)
                            {
                                Dictionary<string, object> et_acpr1 = new Dictionary<string, object>();
                                Keysight.CommunicationsFabric.Protocol.ArgMap etAcpr1Value = new Keysight.CommunicationsFabric.Protocol.ArgMap();
                                etAcpr1Value["DELAY"] = x;
                                etAcpr1Value["ACPR_L1"] = y1;
                                etAcpr1Value["ACPR_H1"] = y2;
                                et_acpr1[MeasurementDataStructure.Value] = etAcpr1Value;
                                et_acpr1[MeasurementDataStructure.IsSubMeas] = true;
                                et_acpr1[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                                et_acpr1[MeasurementDataStructure.SubMeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.ET_ACPR1);
                                et_acpr1[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                                MeasurementDataCommunicationHelper.PublishRealTimeResults(et_acpr1);
                            }

                            Array delay = (Array)x;
                            Array acpr_l1 = (Array)y1;
                            Array acpr_h1 = (Array)y2;
                            for (int i = 0; i < delay.Length; i++)
                            {
                                Results.Publish("ACPR(ET)", new List<string> { "IQ Delay", "Average ACPR" },
                                    (double)delay.GetValue(i),
                                    ((double)acpr_l1.GetValue(i) + (double)acpr_h1.GetValue(i)) / 2);
                            }

                            //GetAlignData2
                            object delay_2 = null;
                            object acprL1_2 = null;
                            object acprH1_2 = null;
                            ss.GetAlignData2(ref delay_2, ref acprL1_2, ref acprH1_2);
                            if (GetInstrument().MeasConfigDict[MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.ET)].EnableRealTime)
                            {
                                Dictionary<string, object> et_acpr2 = new Dictionary<string, object>();
                                Keysight.CommunicationsFabric.Protocol.ArgMap acpr2_value = new Keysight.CommunicationsFabric.Protocol.ArgMap();
                                acpr2_value["DELAY"] = delay_2;
                                acpr2_value["ACPR_L1"] = acprL1_2;
                                acpr2_value["ACPR_H1"] = acprH1_2;
                                et_acpr2[MeasurementDataStructure.Value] = acpr2_value;
                                et_acpr2[MeasurementDataStructure.IsSubMeas] = true;
                                et_acpr2[MeasurementDataStructure.Frequency] = GetInstrument().selected_frequency;
                                et_acpr2[MeasurementDataStructure.SubMeasName] = MeasurementDataCommunicationHelper.TestItemName(TestItem_Enum.ET_ACPR2);
                                et_acpr2[MeasurementDataStructure.Technology] = GetInstrument().selected_technology;
                                MeasurementDataCommunicationHelper.PublishRealTimeResults(et_acpr2);
                            }


                        }
                    }
                }
                else
                {
                    Logging(LogLevel.WARNING, "Apply ET called but ET is not enabled!");
                }

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "Apply ET Fail: {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        private void GenerateDpdResult(string title, ref Array am_x, ref Array am_y, ref Array pm_y)
        {
            if (generate_amam_ampm)
            {
                ss.GetAmamAmpmData(ss.reference, ss.measured, ref am_x, ref am_y, ref pm_y);

                for (int i = 0; i < am_x.Length; i++)
                {
                    Results.Publish(title, new List<string> { "AM_X", "AM_Y", "PM_Y" },
                    new IConvertible[] { am_x.GetValue(i).ToString(), am_y.GetValue(i).ToString(), pm_y.GetValue(i).ToString() });
                }

            }
        }

    }

    //[Display(Name: "Apply DPD", Groups: new string[] { "PA", "ET/DPD" })]
    //[Description("Apply DPD")]
    //public class PA_ApplyDpd : PA_EtDpdCommon
    //{

    //    public PA_ApplyDpd()
    //    {
    //    }

    //    public override void PrePlanRun()
    //    {
    //        base.PrePlanRun();
    //        // ToDo: Optionally add any setup code this steps needs to run before the testplan starts
    //    }

    //    public override void Run()
    //    {
    //        // IMPORTANT NOTE: Must add test case code AFTER RunChildSteps()
    //        // ToDo: Add test case code here
    //        bool ret = true;

    //        try
    //        {
    //            if (GetInstrument().dpd_enabled)
    //            {
    //                Array am_x = null;
    //                Array am_y = null;
    //                Array pm_y = null;

    //                ret = Init();

    //                ss.useDpdForEnv = GetInstrument().dpd_enabled && GetInstrument().et_enabled;

    //                object measured = null;
    //                object lut_or_coef = null;
    //                object predistorted = null;

    //                meas.TestConditionSetup_Trigger(false, true, 100e-6, AnalyzerTriggerModeEnum.AcquisitionTriggerModeExternal, 
    //                    0, AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
    //                    100, AnalyzerTriggerEnum.TriggerPXITrigger2, true);

    //                // Extract Am/Am Am/Pm Data before doing DPD
    //                ss.CreateMeasuredData(ss.dpdEvalTime, true, ref measured);
    //                ss.ExtractDpdModel(ss.reference, measured, out predistorted, ref lut_or_coef, false);
    //                GenerateDpdResult("Pre-DPD", ref am_x, ref am_y, ref pm_y);

    //                // Apply the DPD Model to the waveforms
    //                if (predistorted != null)
    //                    ss.ApplyDpdModel(predistorted);

    //                if (ss.etEnabled)
    //                {
    //                    // Start the Envelope waveform
    //                    vsg.StopModulation();
    //                    if (ss.useDpdForEnv)
    //                        awg.PlayWaveform(ss.etDpdFileName, true);
    //                    else
    //                        awg.PlayWaveform(ss.etCfFileName, true);

    //                    // Setup VSG and VSA and start the RF waveform playback 
    //                    vsg.PlayARB(ss.iqDpdFileName, false, ss.sigStudioWaveformLength, true);

    //                    // Wait 1 waveform cycle or a minimum of 5 ms
    //                    System.Threading.Thread.Sleep((int)(Math.Max(5, ss.sigStudioWaveformLength * 1000 + 1)));
    //                }
    //                else
    //                {
    //                    vsg.PlayARB(ss.iqDpdFileName, false, ss.sigStudioWaveformLength, true);
    //                }

    //                // Extract Am/Am Am/Pm Data after doing DPD
    //                ss.CreateMeasuredData(ss.dpdEvalTime, true, ref measured);
    //                ss.ExtractDpdModel(ss.reference, measured, out predistorted, ref lut_or_coef, false);
    //                GenerateDpdResult("Post-DPD", ref am_x, ref am_y, ref pm_y);
    //            }
    //            else
    //            {
    //                Logging(LogLevel.WARNING, "Apply DPD is called but DPD is not enabled!");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Logging(LogLevel.ERROR, "Apply DPD Fail: {0}", ex.Message);
    //            ret = false;
    //        }

    //        UpgradeVerdict(ret ? Verdict.Pass : Verdict.Fail);

    //        // If no verdict is used, the verdict will default to NotSet.
    //        // You can change the verdict using UpgradeVerdict() as shown below.
    //    }

    //    public override void PostPlanRun()
    //    {
    //        base.PostPlanRun();
    //        // ToDo: Optionally add any cleanup code this steps needs to run after the entire testplan has finished
    //    }

    //    private void GenerateDpdResult(string title, ref Array am_x, ref Array am_y, ref Array pm_y)
    //    {
    //        if (generate_amam_ampm)
    //        {
    //            ss.GetAmamAmpmData(ss.reference, ss.measured, ref am_x, ref am_y, ref pm_y);

    //            for (int i = 0; i<am_x.Length; i++)
    //            {
    //                Results.Publish(title, new List<string> { "AM_X", "AM_Y", "PM_Y" },
    //                new IConvertible[] { am_x.GetValue(i).ToString(), am_y.GetValue(i).ToString(), pm_y.GetValue(i).ToString() });
    //            }

    //        }
    //    }
    //}

    #endregion

    #region IQ Capture
    [Display(Name: "IQ Data Get", Groups: new string[] { "PA", "Measurements" })]
    public class PA_IQDataGet : PA_TestStep
    {
        #region Settings
        [Display(Name: "Set Receiver Range", Group: "Receiver Setup", Order: 10.1)]
        [Browsable(true)]
        public bool set_range { get; set; }

        [Display(Name: "Receiver Range", Group: "Receiver Setup", Order: 10.2)]
        [Unit("dBm")]
        [Browsable(true)]
        [EnabledIf("set_range", true, HideIfDisabled = true)]
        public double rcv_range { get; set; }

        [Display(Name: "Set Receiver Frequency", Group: "Receiver Setup", Order: 10.3)]
        [Browsable(true)]
        public bool set_frequency { get; set; }

        [Display(Name: "Frequency", Group: "Receiver Setup", Order: 10.4)]
        [Unit("MHz")]
        [EnabledIf("set_frequency", true, HideIfDisabled = true)]
        public double rcv_frequency { get; set; }

 
        [Display(Name: "IQ for measurement", Group: "Measurement IQ Data Setup", Order: 1)]
        public IQ_GetForMeasurement measurement_type { get; set; }

        [Display(Name: "Sampling Rate", Group: "Measurement IQ Data Setup", Order: 2.1)]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.None, HideIfDisabled = true)]
        public double sampling_rate { get; set; }

        [Display(Name: "Duration", Group: "Measurement IQ Data Setup", Order: 2.2)]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.None, HideIfDisabled = true)]
        [Unit("ms")]
        public double sampling_duration { get; set; }

        [Display(Name: "Trigger Source", Group: "Measurement IQ Data Setup", Order: 3.1)]
        [Browsable(true)]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.SEM, HideIfDisabled = true)]
        public SEMTRIGGERMODE triggersource { get; set; }

        [Display(Name: "Trigger Delay", Group: "Measurement IQ Data Setup", Order: 3.2)]
        [Browsable(true)]
        [Unit("s")]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.SEM, HideIfDisabled = true)]
        public double triggerdelay { get; set; }

        [Display(Name: "Trigger Level (RF Burst)", Group: "Measurement IQ Data Setup", Order: 3.3)]
        [Browsable(true)]
        [Unit("dBm")]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.SEM, HideIfDisabled = true)]
        public double triggerlevel_dbm { get; set; }

        [Display(Name: "Trigger Level (External)", Group: "Measurement IQ Data Setup", Order: 3.4)]
        [Browsable(true)]
        [Unit("volt")]
        [EnabledIf("measurement_type", IQ_GetForMeasurement.SEM, HideIfDisabled = true)]
        public double triggerlevel_v { get; set; }

        #endregion

        public double[] IQ_Data;
        private PlugInKMF plugInKmf = null;
        private SemUserConfigure user_setting = new SemUserConfigure();

        public PA_IQDataGet()
        {
            set_range = false;
            rcv_range = 0.0;
            set_frequency = false;
            rcv_frequency = 1700;

            measurement_type = IQ_GetForMeasurement.None;
            sampling_rate = 30.72e6;
            sampling_duration = 2;

            // SEM
            triggersource = SEMTRIGGERMODE.Immediate;
            triggerlevel_v = 2.0;
            triggerlevel_dbm = -20;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            plugInKmf = GetInstrument().pluginKmf;
        }

        public override void Run()
        {
            bool pass = true;
            try
            {
                PA_Instrument pi = GetInstrument();
                PlugInMeas ps = pi.Measurement;
                IVSG vsg = null;
                IVSA vsa = null;
                IVXT vxt = null;
                bool secondary_source_analyzer = false;
                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY;
                ps.setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, secondary_source_analyzer, secondary_source_analyzer_config);

                if (set_range && GetInstrument().vsa != null)
                {
                    Logging(LogLevel.INFO, "Receiver range adjust from {0}dBm to {1}dBm", GetInstrument().vsa.Power, rcv_range);
                    vsa.Power = rcv_range;
                    vsa.Apply();
                }
                if (set_frequency && GetInstrument().vsa != null)
                {
                    Logging(LogLevel.INFO, "Receiver frequency adjust from {0}dBm to {1}dBm", GetInstrument().vsa.Frequency, rcv_frequency);
                    vsa.Frequency = rcv_frequency * 1e6;
                    vsa.Apply();
                }

                if (measurement_type == IQ_GetForMeasurement.EVM)
                {
                    vsa.GetIQData(ref IQ_Data, vsa.INIInfo.EvmSamplingRate, vsa.INIInfo.EvmSamplingDuration);
                }
                else if (measurement_type == IQ_GetForMeasurement.SEM)
                {
                    DIRECTION dir = DIRECTION.DOWNLINK;
                    ps.TestConditionSetup_GetWaveformInfo(GetInstrument().selected_technology,
                        ref user_setting.tech_format,
                        ref user_setting.bandwidth,
                        ref user_setting.modtype,
                        ref dir);
                    SemAcqUserSetting();

                    double channle_sample_rate = 0;
                    pass = ps.KmfMeasureSEM_Acq(user_setting, ref IQ_Data, ref channle_sample_rate);
                    sampling_rate = channle_sample_rate;

                    GetInstrument().vsa.IQInfo.power = GetInstrument().vsa.Power;
                    GetInstrument().vsa.IQInfo.frequency = GetInstrument().selected_frequency * 1e6;
                    GetInstrument().vsa.IQInfo.tech = GetInstrument().selected_technology;
                }
                else // Generic
                {
                    vsa.GetIQData(ref IQ_Data, sampling_rate, sampling_duration * 0.001);
                }

                lock (vsa.IQInfo)
                {
                    vsa.IQInfo.power = vsa.Power;
                    vsa.IQInfo.frequency = pi.selected_frequency * 1e6;
                    vsa.IQInfo.tech = pi.selected_technology;
                    vsa.IQInfo.sample_rate = sampling_rate;

                    if (vsa.IQInfo.IQ == null) vsa.IQInfo.IQ = new double[IQ_Data.Length];
                    else Array.Resize(ref vsa.IQInfo.IQ, IQ_Data.Length);

                    Buffer.BlockCopy(IQ_Data, 0, vsa.IQInfo.IQ, 0, IQ_Data.Length * 8);
                }

                RunChildSteps(); //If step has child steps.

                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                    UpgradeVerdict(Verdict.Pass);
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                    Logging(LogLevel.ERROR, "IQ GetData Failure: " + GetInstrument().pluginKmf.evmErrorMsg);
                }
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
                Logging(LogLevel.ERROR, "IQ GetData Failure: " + ex.Message);
            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();

        }
        private void SemAcqUserSetting()
        {
            user_setting.frequecy = GetInstrument().selected_frequency * 1e6;
            user_setting.triggersrc = triggersource;
            user_setting.triggerdelay = triggerdelay;
            user_setting.triggerlevel = (triggersource == SEMTRIGGERMODE.External) ? triggerlevel_v : triggerlevel_dbm;
        }
    }

    #endregion

    #region Vsa 89600
    [Display(Name: "EVM by VSA 89600", Groups: new string[] { "PA", "Measurements (VSA 89600)" }, Order: 1)]
    [Description("EVM by VSA 89600")]
    [Browsable(true)]
    public class PA_EVM_VSA : PA_TestStep
    {
        #region Settings

        [Display(Name: "Center Frequency", Order: 1)]
        [Browsable(true)]
        [Unit("MHz")]
        public double center_freq { get; set; }

        [Display(Name: "Show VSA 89600", Order: 3)]
        [Browsable(true)]
        public bool IsVsaVisible { get; set; }

        [Display(Name: "Show Spectrum", Order: 4)]
        [Browsable(true)]
        public bool isShowSpectrum { get; set; }

        [Display(Name: "Show IQ Modulation", Order: 5)]
        [Browsable(true)]
        public bool isShowIQMeas { get; set; }

        #endregion



        public PA_EVM_VSA()
        {
            center_freq = 1700;
            IsVsaVisible = true;
            isShowIQMeas = true;
            isShowSpectrum = true;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();

        }

        public override void Run()
        {
            bool pass = true;
            PlugInMeas meas = GetInstrument().Measurement;

            GetInstrument().pluginVsa89600?.SetVisibility(IsVsaVisible);

            double evm = new double();
            try
            {

                if (meas != null)
                {
                    //meas.Restart89600();
                    if (GetInstrument().trx_config == PRIMARY_SOURCE_ANALYZER_CONFIG.VSA_AND_VSG)
                        pass = meas.DoVsaMeasurements(false, GetInstrument().selected_technology, center_freq, ref evm, true, isShowIQMeas, isShowSpectrum);
                    else
                        pass = meas.DoVsaMeasurements(true, GetInstrument().selected_technology, center_freq, ref evm, true, isShowIQMeas, isShowSpectrum);
                }

                RunChildSteps(); //If step has child steps.

                Log.Info("{0}, EVM {1}", GetInstrument().selected_technology, evm);
                // If no verdict is used, the verdict will default to NotSet.
                // You can change the verdict using UpgradeVerdict() as shown below.
                // UpgradeVerdict(Verdict.Pass);
                if (pass)
                {
                    UpgradeVerdict(Verdict.Pass);
                }
                else
                {
                    UpgradeVerdict(Verdict.Fail);
                }
            }
            catch (Exception ex)
            {
                UpgradeVerdict(Verdict.Fail);
                Log.Error(ex.Message);

            }
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();

        }
    }
    #endregion

    #endregion

    //#region STDF Result Listener

    //public static class STDF
    //{
    //    private static IConvertible ToConvertible(BitArray val)
    //    {
    //        StringBuilder sb = new StringBuilder();

    //        sb.Append(val.Count.ToString());

    //        for (int i = 0; i < val.Count; i++)
    //            if (val[i])
    //                sb.Append(string.Format(",{0}", i));

    //        return sb.ToString();
    //    }

    //    private static IConvertible JoinArray<T>(T[] val)
    //    {
    //        if (val == null)
    //            return "";
    //        else
    //            return string.Join(",", val.Select(x => Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture)));
    //    }

    //    private static string DeNull(string t)
    //    {
    //        if (t == null)
    //            return "";
    //        else
    //            return t;
    //    }

    //    //public static TestStepResultType FarType = new TestStepResultType { Name = "STDF Far", DimensionTitles = new List<string> { "CpuType", "StdfVersion" } };
    //    //public static TestStepResultType AtrType = new TestStepResultType { Name = "STDF Atr", DimensionTitles = new List<string> { "ModifiedTime", "CommandLine" } };
    //    //public static TestStepResultType MirType = new TestStepResultType { Name = "STDF Mir", DimensionTitles = new List<string> { "SetupTime", "StartTime", "StationNumber", "ModeCode", "RetestCode", "ProtectionCode", "BurnInTime", "CommandModeCode", "LotId", "PartType", "NodeName", "TesterType", "JobName", "JobRevision", "SublotId", "OperatorName", "ExecType", "ExecVersion", "TestCode", "TestTemperature", "UserText", "AuxiliaryFile", "PackageType", "FamilyId", "DateCode", "FacilityId", "FloorId", "ProcessId", "OperationFrequency", "SpecificationName", "SpecificationVersion", "FlowId", "SetupId", "DesignRevision", "EngineeringId", "RomCode", "SerialNumber", "SupervisorName" } };
    //    //public static TestStepResultType MrrType = new TestStepResultType { Name = "STDF Mrr", DimensionTitles = new List<string> { "FinishTime", "DispositionCode", "UserDescription", "ExecDescription" } };
    //    //public static TestStepResultType PcrType = new TestStepResultType { Name = "STDF Pcr", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber", "PartCount", "RetestCount", "AbortCount", "GoodCount", "FunctionalCount" } };
    //    //public static TestStepResultType HbrType = new TestStepResultType { Name = "STDF Hbr", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber", "BinNumber", "BinCount", "BinPassFail", "BinName" } };
    //    //public static TestStepResultType SbrType = new TestStepResultType { Name = "STDF Sbr", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber", "BinNumber", "BinCount", "BinPassFail", "BinName" } };
    //    //public static TestStepResultType PmrType = new TestStepResultType { Name = "STDF Pmr", DimensionTitles = new List<string> { "Index", "ChannelType", "ChannelName", "PhysicalName", "LogicalName", "HeadNumber", "SiteNumber" } };
    //    //public static TestStepResultType PgrType = new TestStepResultType { Name = "STDF Pgr", DimensionTitles = new List<string> { "GroupIndex", "GroupName", "PinIndexes" } };
    //    //public static TestStepResultType RdrType = new TestStepResultType { Name = "STDF Rdr", DimensionTitles = new List<string> { "RetestBins" } };
    //    //public static TestStepResultType SdrType = new TestStepResultType { Name = "STDF Sdr", DimensionTitles = new List<string> { "HeadNumber", "SiteGroup", "SiteNumbers", "HandlerType", "HandlerId", "CardType", "CardId", "LoadboardType", "LoadboardId", "DibType", "DibId", "CableType", "CableId", "ContactorType", "ContactorId", "LaserType", "LaserId", "ExtraType", "ExtraId" } };
    //    //public static TestStepResultType WirType = new TestStepResultType { Name = "STDF Wir", DimensionTitles = new List<string> { "HeadNumber", "SiteGroup", "StartTime", "WaferId" } };
    //    //public static TestStepResultType WrrType = new TestStepResultType { Name = "STDF Wrr", DimensionTitles = new List<string> { "HeadNumber", "SiteGroup", "FinishTime", "PartCount", "RetestCount", "AbortCount", "GoodCount", "FunctionalCount", "WaferId", "FabWaferId", "FrameId", "MaskId", "UserDescription", "ExecDescription" } };
    //    //public static TestStepResultType WcrType = new TestStepResultType { Name = "STDF Wcr", DimensionTitles = new List<string> { "WaferSize", "DieHeight", "DieWidth", "Units", "Flat", "CenterX", "CenterY", "PositiveX", "PositiveY" } };
    //    //public static TestStepResultType PirType = new TestStepResultType { Name = "STDF Pir", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber" } };
    //    //public static TestStepResultType PrrType = new TestStepResultType { Name = "STDF Prr", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber", "PartFlag", "TestCount", "HardBin", "SoftBin", "XCoordinate", "YCoordinate", "TestTime", "PartId", "PartText", "PartFix", "SupersedesPartId", "SupersedesCoords", "AbnormalTest", "Failed" } };
    //    //public static TestStepResultType TsrType = new TestStepResultType { Name = "STDF Tsr", DimensionTitles = new List<string> { "HeadNumber", "SiteNumber", "TestType", "TestNumber", "ExecutedCount", "FailedCount", "AlarmCount", "TestName", "SequencerName", "TestLabel", "TestTime", "TestMin", "TestMax", "TestSum", "TestSumOfSquares" } };
    //    //public static TestStepResultType PtrType = new TestStepResultType { Name = "STDF Ptr", DimensionTitles = new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "ParametricFlags", "Result", "TestText", "AlarmId", "OptionalFlags", "ResultScalingExponent", "LowLimitScalingExponent", "HighLimitScalingExponent", "LowLimit", "HighLimit", "Units", "ResultFormatString", "LowLimitFormatString", "HighLimitFormatString", "LowSpecLimit", "HighSpecLimit" } };
    //    //public static TestStepResultType MprType = new TestStepResultType { Name = "STDF Mpr", DimensionTitles = new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "ParametricFlags", "PinStates", "Results", "TestText", "AlarmId", "OptionalFlags", "ResultScalingExponent", "LowLimitScalingExponent", "HighLimitScalingExponent", "LowLimit", "HighLimit", "StartingCondition", "ConditionIncrement", "PinIndexes", "Units", "IncrementUnits", "ResultFormatString", "LowLimitFormatString", "HighLimitFormatString", "LowSpecLimit", "HighSpecLimit" } };
    //    //public static TestStepResultType FtrType = new TestStepResultType { Name = "STDF Ftr", DimensionTitles = new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "CycleCount", "RelativeVectorAddress", "RepeatCount", "FailingPinCount", "XFailureAddress", "YFailureAddress", "VectorOffset", "ReturnIndexes", "ReturnStates", "ProgrammedIndexes", "ProgrammedStates", "FailingPinBitfield", "VectorName", "TimeSet", "OpCode", "TestText", "AlarmId", "ProgrammedText", "ResultText", "PatternGeneratorNumber", "SpinMap" } };
    //    //public static TestStepResultType EpsType = new TestStepResultType { Name = "STDF Eps", DimensionTitles = new List<string> { } };
    //    //public static TestStepResultType DtrType = new TestStepResultType { Name = "STDF Dtr", DimensionTitles = new List<string> { "Text" } };

    //    public static void AddFar(this ResultSource results, Byte CpuType, Byte StdfVersion)
    //    {
    //        results.Publish("STDF Far", new List<string> { "CpuType", "StdfVersion" },
    //            new IConvertible[] { CpuType, StdfVersion });
    //    }

    //    public static void AddAtr(this ResultSource results, String CommandLine, DateTime? ModifiedTime = null)
    //    {
    //        results.Publish("STDF Atr", new List<string> { "ModifiedTime", "CommandLine" },
    //            new IConvertible[] { ModifiedTime, DeNull(CommandLine) });
    //    }

    //    public static void AddMir(this ResultSource results, Byte StationNumber, String ModeCode, String RetestCode, String ProtectionCode, String CommandModeCode, DateTime? SetupTime = null, DateTime? StartTime = null, UInt16? BurnInTime = null, String LotId = null, String PartType = null, String NodeName = null, String TesterType = null, String JobName = null, String JobRevision = null, String SublotId = null, String OperatorName = null, String ExecType = null, String ExecVersion = null, String TestCode = null, String TestTemperature = null, String UserText = null, String AuxiliaryFile = null, String PackageType = null, String FamilyId = null, String DateCode = null, String FacilityId = null, String FloorId = null, String ProcessId = null, String OperationFrequency = null, String SpecificationName = null, String SpecificationVersion = null, String FlowId = null, String SetupId = null, String DesignRevision = null, String EngineeringId = null, String RomCode = null, String SerialNumber = null, String SupervisorName = null)
    //    {
    //        results.Publish("STDF Mir", new List<string> { "SetupTime", "StartTime", "StationNumber", "ModeCode", "RetestCode", "ProtectionCode", "BurnInTime",
    //            "CommandModeCode", "LotId", "PartType", "NodeName", "TesterType", "JobName", "JobRevision", "SublotId", "OperatorName", "ExecType", "ExecVersion",
    //            "TestCode", "TestTemperature", "UserText", "AuxiliaryFile", "PackageType", "FamilyId", "DateCode", "FacilityId", "FloorId", "ProcessId", "OperationFrequency",
    //            "SpecificationName", "SpecificationVersion", "FlowId", "SetupId", "DesignRevision", "EngineeringId", "RomCode", "SerialNumber", "SupervisorName" },
    //            new IConvertible[] { SetupTime, StartTime, StationNumber, DeNull(ModeCode), DeNull(RetestCode), DeNull(ProtectionCode), BurnInTime,
    //                DeNull(CommandModeCode), DeNull(LotId), DeNull(PartType), DeNull(NodeName), DeNull(TesterType), DeNull(JobName), DeNull(JobRevision), DeNull(SublotId), DeNull(OperatorName), DeNull(ExecType), DeNull(ExecVersion),
    //                DeNull(TestCode), DeNull(TestTemperature), DeNull(UserText), DeNull(AuxiliaryFile), DeNull(PackageType), DeNull(FamilyId), DeNull(DateCode), DeNull(FacilityId), DeNull(FloorId), DeNull(ProcessId), DeNull(OperationFrequency),
    //                DeNull(SpecificationName), DeNull(SpecificationVersion), DeNull(FlowId), DeNull(SetupId), DeNull(DesignRevision), DeNull(EngineeringId), DeNull(RomCode), DeNull(SerialNumber), DeNull(SupervisorName)});
    //    }

    //    public static void AddMrr(this ResultSource results, String DispositionCode, DateTime? FinishTime = null, String UserDescription = null, String ExecDescription = null)
    //    {
    //        results.Publish("STDF Mrr", new List<string> { "FinishTime", "DispositionCode", "UserDescription", "ExecDescription" },
    //            new IConvertible[] { FinishTime, DeNull(DispositionCode), DeNull(UserDescription), DeNull(ExecDescription) });
    //    }

    //    public static void AddPcr(this ResultSource results, UInt32 PartCount, Byte? HeadNumber = null, Byte? SiteNumber = null, UInt32? RetestCount = null, UInt32? AbortCount = null, UInt32? GoodCount = null, UInt32? FunctionalCount = null)
    //    {
    //        results.Publish("STDF Pcr", new List<string> { "HeadNumber", "SiteNumber", "PartCount", "RetestCount", "AbortCount", "GoodCount", "FunctionalCount" },
    //            new IConvertible[] { HeadNumber, SiteNumber, PartCount, RetestCount, AbortCount, GoodCount, FunctionalCount });
    //    }

    //    public static void AddHbr(this ResultSource results, UInt16 BinNumber, UInt32 BinCount, String BinPassFail, Byte? HeadNumber = null, Byte? SiteNumber = null, String BinName = null)
    //    {
    //        results.Publish("STDF Hbr", new List<string> { "HeadNumber", "SiteNumber", "BinNumber", "BinCount", "BinPassFail", "BinName" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)BinNumber, (IConvertible)BinCount, DeNull(BinPassFail), DeNull(BinName) });
    //    }

    //    public static void AddSbr(this ResultSource results, UInt16 BinNumber, UInt32 BinCount, String BinPassFail, Byte? HeadNumber = null, Byte? SiteNumber = null, String BinName = null)
    //    {
    //        results.Publish("STDF Sbr", new List<string> { "HeadNumber", "SiteNumber", "BinNumber", "BinCount", "BinPassFail", "BinName" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)BinNumber, (IConvertible)BinCount, DeNull(BinPassFail), DeNull(BinName) });
    //    }

    //    public static void AddPmr(this ResultSource results, UInt16 Index, UInt16? ChannelType = null, String ChannelName = null, String PhysicalName = null, String LogicalName = null, Byte? HeadNumber = null, Byte? SiteNumber = null)
    //    {
    //        results.Publish("STDF Pmr", new List<string> { "Index", "ChannelType", "ChannelName", "PhysicalName", "LogicalName", "HeadNumber", "SiteNumber" },
    //            new IConvertible[] { (IConvertible)Index, (IConvertible)ChannelType, DeNull(ChannelName), DeNull(PhysicalName), DeNull(LogicalName), (IConvertible)HeadNumber, (IConvertible)SiteNumber });
    //    }

    //    public static void AddPgr(this ResultSource results, UInt16 GroupIndex, String GroupName = null, UInt16[] PinIndexes = null)
    //    {
    //        results.Publish("STDF Pgr", new List<string> { "GroupIndex", "GroupName", "PinIndexes" },
    //            new IConvertible[] { (IConvertible)GroupIndex, DeNull(GroupName), JoinArray<UInt16>(PinIndexes) });
    //    }

    //    public static void AddRdr(this ResultSource results, UInt16[] RetestBins = null)
    //    {
    //        results.Publish("STDF Rdr", new List<string> { "RetestBins" },
    //            new IConvertible[] { JoinArray<UInt16>(RetestBins) });
    //    }

    //    public static void AddSdr(this ResultSource results, Byte? HeadNumber = null, Byte? SiteGroup = null, Byte[] SiteNumbers = null, String HandlerType = null, String HandlerId = null, String CardType = null, String CardId = null, String LoadboardType = null, String LoadboardId = null, String DibType = null, String DibId = null, String CableType = null, String CableId = null, String ContactorType = null, String ContactorId = null, String LaserType = null, String LaserId = null, String ExtraType = null, String ExtraId = null)
    //    {
    //        results.Publish("STDF Sdr", new List<string> { "HeadNumber", "SiteGroup", "SiteNumbers", "HandlerType", "HandlerId", "CardType",
    //            "CardId", "LoadboardType", "LoadboardId", "DibType", "DibId", "CableType", "CableId", "ContactorType", "ContactorId", "LaserType", "LaserId", "ExtraType", "ExtraId" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteGroup, JoinArray<Byte>(SiteNumbers), DeNull(HandlerType), DeNull(HandlerId), DeNull(CardType), DeNull(CardId),
    //                DeNull(LoadboardType), DeNull(LoadboardId), DeNull(DibType), DeNull(DibId), DeNull(CableType), DeNull(CableId), DeNull(ContactorType), DeNull(ContactorId), DeNull(LaserType), DeNull(LaserId), DeNull(ExtraType), DeNull(ExtraId) });
    //    }

    //    public static void AddWir(this ResultSource results, Byte? HeadNumber = null, Byte? SiteGroup = null, DateTime? StartTime = null, String WaferId = null)
    //    {
    //        results.Publish("STDF Wir", new List<string> { "HeadNumber", "SiteGroup", "StartTime", "WaferId" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteGroup, (IConvertible)StartTime, DeNull(WaferId) });
    //    }

    //    public static void AddWrr(this ResultSource results, UInt32 PartCount, Byte? HeadNumber = null, Byte? SiteGroup = null, DateTime? FinishTime = null, UInt32? RetestCount = null, UInt32? AbortCount = null, UInt32? GoodCount = null, UInt32? FunctionalCount = null, String WaferId = null, String FabWaferId = null, String FrameId = null, String MaskId = null, String UserDescription = null, String ExecDescription = null)
    //    {
    //        results.Publish("STDF Wrr", new List<string> { "HeadNumber", "SiteGroup", "FinishTime", "PartCount", "RetestCount", "AbortCount", "GoodCount", "FunctionalCount", "WaferId", "FabWaferId", "FrameId", "MaskId", "UserDescription", "ExecDescription" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteGroup, (IConvertible)FinishTime, (IConvertible)PartCount, (IConvertible)RetestCount, (IConvertible)AbortCount, (IConvertible)GoodCount, (IConvertible)FunctionalCount, DeNull(WaferId), DeNull(FabWaferId), DeNull(FrameId), DeNull(MaskId), DeNull(UserDescription), DeNull(ExecDescription) });
    //    }

    //    public static void AddWcr(this ResultSource results, String Flat, String PositiveX, String PositiveY, Single? WaferSize = null, Single? DieHeight = null, Single? DieWidth = null, Byte? Units = null, Int16? CenterX = null, Int16? CenterY = null)
    //    {
    //        results.Publish("STDF Wcr", new List<string> { "WaferSize", "DieHeight", "DieWidth", "Units", "Flat", "CenterX", "CenterY", "PositiveX", "PositiveY" },
    //            new IConvertible[] { (IConvertible)WaferSize, (IConvertible)DieHeight, (IConvertible)DieWidth, (IConvertible)Units, DeNull(Flat), (IConvertible)CenterX, (IConvertible)CenterY, DeNull(PositiveX), DeNull(PositiveY) });
    //    }

    //    public static void AddPir(this ResultSource results, Byte? HeadNumber = null, Byte? SiteNumber = null)
    //    {
    //        results.Publish("STDF Pir", new List<string> { "HeadNumber", "SiteNumber" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteNumber });
    //    }

    //    public static void AddPrr(this ResultSource results, Byte PartFlag, UInt16 TestCount, UInt16 HardBin, Boolean SupersedesPartId, Boolean SupersedesCoords, Boolean AbnormalTest, Byte? HeadNumber = null, Byte? SiteNumber = null, UInt16? SoftBin = null, Int16? XCoordinate = null, Int16? YCoordinate = null, UInt32? TestTime = null, String PartId = null, String PartText = null, Byte[] PartFix = null, Boolean? Failed = null)
    //    {
    //        results.Publish("STDF Prr", new List<string> { "HeadNumber", "SiteNumber", "PartFlag", "TestCount", "HardBin", "SoftBin", "XCoordinate", "YCoordinate",
    //            "TestTime", "PartId", "PartText", "PartFix", "SupersedesPartId", "SupersedesCoords", "AbnormalTest", "Failed" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)PartFlag, (IConvertible)TestCount, (IConvertible)HardBin, (IConvertible)SoftBin,
    //                (IConvertible)XCoordinate, (IConvertible)YCoordinate, (IConvertible)TestTime, DeNull(PartId), DeNull(PartText), JoinArray<Byte>(PartFix), (IConvertible)SupersedesPartId,
    //                (IConvertible)SupersedesCoords, (IConvertible)AbnormalTest, (IConvertible)Failed });
    //    }

    //    public static void AddTsr(this ResultSource results, String TestType, UInt32 TestNumber, Byte? HeadNumber = null, Byte? SiteNumber = null, UInt32? ExecutedCount = null, UInt32? FailedCount = null, UInt32? AlarmCount = null, String TestName = null, String SequencerName = null, String TestLabel = null, Single? TestTime = null, Single? TestMin = null, Single? TestMax = null, Single? TestSum = null, Single? TestSumOfSquares = null)
    //    {
    //        results.Publish("STDF Tsr", new List<string> { "HeadNumber", "SiteNumber", "TestType", "TestNumber", "ExecutedCount", "FailedCount", "AlarmCount", "TestName", "SequencerName", "TestLabel", "TestTime", "TestMin", "TestMax", "TestSum", "TestSumOfSquares" },
    //            new IConvertible[] { (IConvertible)HeadNumber, (IConvertible)SiteNumber, DeNull(TestType), (IConvertible)TestNumber, (IConvertible)ExecutedCount, (IConvertible)FailedCount, (IConvertible)AlarmCount, DeNull(TestName), DeNull(SequencerName), DeNull(TestLabel), (IConvertible)TestTime, (IConvertible)TestMin, (IConvertible)TestMax, (IConvertible)TestSum, (IConvertible)TestSumOfSquares });
    //    }

    //    public static void AddPtr(this ResultSource results, UInt32 TestNumber, Byte TestFlags, Byte ParametricFlags, Byte? HeadNumber = null, Byte? SiteNumber = null, Single? Result = null, String TestText = null, String AlarmId = null, Byte? OptionalFlags = null, SByte? ResultScalingExponent = null, SByte? LowLimitScalingExponent = null, SByte? HighLimitScalingExponent = null, Single? LowLimit = null, Single? HighLimit = null, String Units = null, String ResultFormatString = null, String LowLimitFormatString = null, String HighLimitFormatString = null, Single? LowSpecLimit = null, Single? HighSpecLimit = null)
    //    {
    //        results.Publish("STDF Ptr", new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "ParametricFlags", "Result", "TestText", "AlarmId", "OptionalFlags", "ResultScalingExponent", "LowLimitScalingExponent", "HighLimitScalingExponent", "LowLimit", "HighLimit", "Units", "ResultFormatString", "LowLimitFormatString", "HighLimitFormatString", "LowSpecLimit", "HighSpecLimit" },
    //            new IConvertible[] { (IConvertible)TestNumber, (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)TestFlags, (IConvertible)ParametricFlags, (IConvertible)Result, DeNull(TestText), DeNull(AlarmId), (IConvertible)OptionalFlags, (IConvertible)ResultScalingExponent, (IConvertible)LowLimitScalingExponent, (IConvertible)HighLimitScalingExponent, (IConvertible)LowLimit, (IConvertible)HighLimit, DeNull(Units), DeNull(ResultFormatString), DeNull(LowLimitFormatString), DeNull(HighLimitFormatString), (IConvertible)LowSpecLimit, (IConvertible)HighSpecLimit });
    //    }

    //    public static void AddMpr(this ResultSource results, UInt32 TestNumber, Byte TestFlags, Byte ParametricFlags, Byte? HeadNumber = null, Byte? SiteNumber = null, Byte[] PinStates = null, Single[] Results = null, String TestText = null, String AlarmId = null, Byte? OptionalFlags = null, SByte? ResultScalingExponent = null, SByte? LowLimitScalingExponent = null, SByte? HighLimitScalingExponent = null, Single? LowLimit = null, Single? HighLimit = null, Single? StartingCondition = null, Single? ConditionIncrement = null, UInt16[] PinIndexes = null, String Units = null, String IncrementUnits = null, String ResultFormatString = null, String LowLimitFormatString = null, String HighLimitFormatString = null, Single? LowSpecLimit = null, Single? HighSpecLimit = null)
    //    {
    //        results.Publish("STDF Mpr", new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "ParametricFlags", "PinStates", "Results", "TestText", "AlarmId", "OptionalFlags", "ResultScalingExponent", "LowLimitScalingExponent", "HighLimitScalingExponent", "LowLimit", "HighLimit", "StartingCondition", "ConditionIncrement", "PinIndexes", "Units", "IncrementUnits", "ResultFormatString", "LowLimitFormatString", "HighLimitFormatString", "LowSpecLimit", "HighSpecLimit" },
    //            new IConvertible[] { (IConvertible)TestNumber, (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)TestFlags, (IConvertible)ParametricFlags, JoinArray<Byte>(PinStates), JoinArray<Single>(Results), DeNull(TestText), DeNull(AlarmId), (IConvertible)OptionalFlags, (IConvertible)ResultScalingExponent, (IConvertible)LowLimitScalingExponent, (IConvertible)HighLimitScalingExponent, (IConvertible)LowLimit, (IConvertible)HighLimit, (IConvertible)StartingCondition, (IConvertible)ConditionIncrement, JoinArray<UInt16>(PinIndexes), DeNull(Units), DeNull(IncrementUnits), DeNull(ResultFormatString), DeNull(LowLimitFormatString), DeNull(HighLimitFormatString), (IConvertible)LowSpecLimit, (IConvertible)HighSpecLimit });
    //    }

    //    public static void AddFtr(this ResultSource results, UInt32 TestNumber, Byte TestFlags, Byte? HeadNumber = null, Byte? SiteNumber = null, UInt32? CycleCount = null, UInt32? RelativeVectorAddress = null, UInt32? RepeatCount = null, UInt32? FailingPinCount = null, Int32? XFailureAddress = null, Int32? YFailureAddress = null, Int16? VectorOffset = null, UInt16[] ReturnIndexes = null, Byte[] ReturnStates = null, UInt16[] ProgrammedIndexes = null, Byte[] ProgrammedStates = null, BitArray FailingPinBitfield = null, String VectorName = null, String OpCode = null, String TestText = null, String AlarmId = null, String ProgrammedText = null, String ResultText = null, Byte? PatternGeneratorNumber = null, BitArray SpinMap = null)
    //    {
    //        results.Publish("STDF Ftr", new List<string> { "TestNumber", "HeadNumber", "SiteNumber", "TestFlags", "CycleCount", "RelativeVectorAddress", "RepeatCount", "FailingPinCount", "XFailureAddress", "YFailureAddress", "VectorOffset", "ReturnIndexes", "ReturnStates", "ProgrammedIndexes", "ProgrammedStates", "FailingPinBitfield", "VectorName", "TimeSet", "OpCode", "TestText", "AlarmId", "ProgrammedText", "ResultText", "PatternGeneratorNumber", "SpinMap" },
    //            new IConvertible[] { (IConvertible)TestNumber, (IConvertible)HeadNumber, (IConvertible)SiteNumber, (IConvertible)TestFlags, (IConvertible)CycleCount, (IConvertible)RelativeVectorAddress, (IConvertible)RepeatCount, (IConvertible)FailingPinCount, (IConvertible)XFailureAddress, (IConvertible)YFailureAddress, (IConvertible)VectorOffset, JoinArray<UInt16>(ReturnIndexes), JoinArray<Byte>(ReturnStates), JoinArray<UInt16>(ProgrammedIndexes), JoinArray<Byte>(ProgrammedStates), ToConvertible(FailingPinBitfield), DeNull(VectorName), DeNull(OpCode), DeNull(TestText), DeNull(AlarmId), DeNull(ProgrammedText), DeNull(ResultText), (IConvertible)PatternGeneratorNumber, ToConvertible(SpinMap) });
    //    }

    //    public static void AddBps(this ResultSource results, String Name = null)
    //    {
    //        results.Publish("STDF Bps", new List<string> { "Name" },
    //            new IConvertible[] { DeNull(Name) });
    //    }

    //    public static void AddEps(this ResultSource results)
    //    {
    //        results.Publish("STDF Eps", new List<string> { },
    //            new IConvertible[] { });
    //    }

    //    public static void AddDtr(this ResultSource results, String Text)
    //    {
    //        results.Publish("STDF Dtr", new List<string> { "Text" },
    //            new IConvertible[] { DeNull(Text) });
    //    }
    //}

    //[Display(Name: "STDF", Group: "PA", Description: "Logs results to STDF files")]
    //[ShortName("STDF(PA)")]
    //public class STDF_ResultListener : ResultListener, IFileResultStore
    //{
    //    public class STDFUtility
    //    {
    //        private static BitArray ToBitarray(IConvertible val)
    //        {
    //            if (val == null) return new BitArray(0);
    //            string str = val.ToString();
    //            if (string.IsNullOrEmpty(str)) return new BitArray(0);

    //            var Ints = str.Split(',').Select(sc => int.Parse(sc)).ToList();

    //            if (Ints.Count < 1) return new BitArray(0);

    //            BitArray b = new BitArray(Ints[0]);
    //            for (int i = 1; i < Ints.Count; i++)
    //                b[Ints[i]] = true;
    //            return b;
    //        }

    //        private static T[] ToArray<T>(IConvertible val)
    //        {
    //            string str = null;
    //            if (val != null) str = val.ToString();

    //            if (string.IsNullOrEmpty(str))
    //                return new T[0];
    //            else
    //                return str.Split(',').Select(x => (T)Convert.ChangeType(x, typeof(T), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
    //        }

    //        public static bool IsSTDFType(ResultTable Typ)
    //        {
    //            return Typ.Name.StartsWith("STDF");
    //        }

    //        public static IEnumerable<StdfRecord> GetRecord(ResultTable Result)
    //        {
    //            for (int Row = 0; Row < Result.Rows; Row++)
    //            {
    //                Func<int, IConvertible> GetDim = (dim) =>
    //                {
    //                    var value = Result.Columns[dim].Data.GetValue(Row);
    //                    return value as IConvertible;
    //                };

    //                switch (Result.Name.Substring(5).Substring(0, 3))
    //                {
    //                    case "Far":
    //                        yield return new LinqToStdf.Records.V4.Far
    //                        {
    //                            CpuType = (Byte)GetDim(0),
    //                            StdfVersion = (Byte)GetDim(1)
    //                        };
    //                        break;

    //                    case "Atr":
    //                        yield return new LinqToStdf.Records.V4.Atr
    //                        {
    //                            ModifiedTime = (DateTime?)GetDim(0),
    //                            CommandLine = (String)GetDim(1)
    //                        };
    //                        break;

    //                    case "Mir":
    //                        yield return new LinqToStdf.Records.V4.Mir
    //                        {
    //                            SetupTime = (DateTime?)GetDim(0),
    //                            StartTime = (DateTime?)GetDim(1),
    //                            StationNumber = (Byte)GetDim(2),
    //                            ModeCode = (String)GetDim(3),
    //                            RetestCode = (String)GetDim(4),
    //                            ProtectionCode = (String)GetDim(5),
    //                            BurnInTime = (UInt16?)GetDim(6),
    //                            CommandModeCode = (String)GetDim(7),
    //                            LotId = (String)GetDim(8),
    //                            PartType = (String)GetDim(9),
    //                            NodeName = (String)GetDim(10),
    //                            TesterType = (String)GetDim(11),
    //                            JobName = (String)GetDim(12),
    //                            JobRevision = (String)GetDim(13),
    //                            SublotId = (String)GetDim(14),
    //                            OperatorName = (String)GetDim(15),
    //                            ExecType = (String)GetDim(16),
    //                            ExecVersion = (String)GetDim(17),
    //                            TestCode = (String)GetDim(18),
    //                            TestTemperature = (String)GetDim(19),
    //                            UserText = (String)GetDim(20),
    //                            AuxiliaryFile = (String)GetDim(21),
    //                            PackageType = (String)GetDim(22),
    //                            FamilyId = (String)GetDim(23),
    //                            DateCode = (String)GetDim(24),
    //                            FacilityId = (String)GetDim(25),
    //                            FloorId = (String)GetDim(26),
    //                            ProcessId = (String)GetDim(27),
    //                            OperationFrequency = (String)GetDim(28),
    //                            SpecificationName = (String)GetDim(29),
    //                            SpecificationVersion = (String)GetDim(30),
    //                            FlowId = (String)GetDim(31),
    //                            SetupId = (String)GetDim(32),
    //                            DesignRevision = (String)GetDim(33),
    //                            EngineeringId = (String)GetDim(34),
    //                            RomCode = (String)GetDim(35),
    //                            SerialNumber = (String)GetDim(36),
    //                            SupervisorName = (String)GetDim(37)
    //                        };
    //                        break;

    //                    case "Mrr":
    //                        yield return new LinqToStdf.Records.V4.Mrr
    //                        {
    //                            FinishTime = (DateTime?)GetDim(0),
    //                            DispositionCode = (String)GetDim(1),
    //                            UserDescription = (String)GetDim(2),
    //                            ExecDescription = (String)GetDim(3)
    //                        };
    //                        break;

    //                    case "Pcr":
    //                        yield return new LinqToStdf.Records.V4.Pcr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1),
    //                            PartCount = (UInt32)GetDim(2),
    //                            RetestCount = (UInt32?)GetDim(3),
    //                            AbortCount = (UInt32?)GetDim(4),
    //                            GoodCount = (UInt32?)GetDim(5),
    //                            FunctionalCount = (UInt32?)GetDim(6)
    //                        };
    //                        break;

    //                    case "Hbr":
    //                        yield return new LinqToStdf.Records.V4.Hbr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1),
    //                            BinNumber = (UInt16)GetDim(2),
    //                            BinCount = (UInt32)GetDim(3),
    //                            BinPassFail = (String)GetDim(4),
    //                            BinName = (String)GetDim(5)
    //                        };
    //                        break;

    //                    case "Sbr":
    //                        yield return new LinqToStdf.Records.V4.Sbr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1),
    //                            BinNumber = (UInt16)GetDim(2),
    //                            BinCount = (UInt32)GetDim(3),
    //                            BinPassFail = (String)GetDim(4),
    //                            BinName = (String)GetDim(5)
    //                        };
    //                        break;

    //                    case "Pmr":
    //                        yield return new LinqToStdf.Records.V4.Pmr
    //                        {
    //                            PinIndex = (UInt16)GetDim(0),
    //                            ChannelType = (UInt16?)GetDim(1),
    //                            ChannelName = (String)GetDim(2),
    //                            PhysicalName = (String)GetDim(3),
    //                            LogicalName = (String)GetDim(4),
    //                            HeadNumber = (Byte?)GetDim(5),
    //                            SiteNumber = (Byte?)GetDim(6)
    //                        }; break;

    //                    case "Pgr":
    //                        yield return new LinqToStdf.Records.V4.Pgr
    //                        {
    //                            GroupIndex = (UInt16)GetDim(0),
    //                            GroupName = (String)GetDim(1),
    //                            PinIndexes = ToArray<UInt16>(GetDim(2))
    //                        };
    //                        break;

    //                    case "Rdr":
    //                        yield return new LinqToStdf.Records.V4.Rdr
    //                        {
    //                            RetestBins = ToArray<UInt16>(GetDim(0))
    //                        };
    //                        break;

    //                    case "Sdr":
    //                        yield return new LinqToStdf.Records.V4.Sdr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteGroup = (Byte?)GetDim(1),
    //                            SiteNumbers = ToArray<Byte>(GetDim(2)),
    //                            HandlerType = (String)GetDim(3),
    //                            HandlerId = (String)GetDim(4),
    //                            CardType = (String)GetDim(5),
    //                            CardId = (String)GetDim(6),
    //                            LoadboardType = (String)GetDim(7),
    //                            LoadboardId = (String)GetDim(8),
    //                            DibType = (String)GetDim(9),
    //                            DibId = (String)GetDim(10),
    //                            CableType = (String)GetDim(11),
    //                            CableId = (String)GetDim(12),
    //                            ContactorType = (String)GetDim(13),
    //                            ContactorId = (String)GetDim(14),
    //                            LaserType = (String)GetDim(15),
    //                            LaserId = (String)GetDim(16),
    //                            ExtraType = (String)GetDim(17),
    //                            ExtraId = (String)GetDim(18)
    //                        }; break;

    //                    case "Wir":
    //                        yield return new LinqToStdf.Records.V4.Wir
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteGroup = (Byte?)GetDim(1),
    //                            StartTime = (DateTime?)GetDim(2),
    //                            WaferId = (String)GetDim(3)
    //                        }; break;

    //                    case "Wrr":
    //                        yield return new LinqToStdf.Records.V4.Wrr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteGroup = (Byte?)GetDim(1),
    //                            FinishTime = (DateTime?)GetDim(2),
    //                            PartCount = (UInt32)GetDim(3),
    //                            RetestCount = (UInt32?)GetDim(4),
    //                            AbortCount = (UInt32?)GetDim(5),
    //                            GoodCount = (UInt32?)GetDim(6),
    //                            FunctionalCount = (UInt32?)GetDim(7),
    //                            WaferId = (String)GetDim(8),
    //                            FabWaferId = (String)GetDim(9),
    //                            FrameId = (String)GetDim(10),
    //                            MaskId = (String)GetDim(11),
    //                            UserDescription = (String)GetDim(12),
    //                            ExecDescription = (String)GetDim(13)
    //                        }; break;

    //                    case "Wcr":
    //                        yield return new LinqToStdf.Records.V4.Wcr
    //                        {
    //                            WaferSize = (Single?)GetDim(0),
    //                            DieHeight = (Single?)GetDim(1),
    //                            DieWidth = (Single?)GetDim(2),
    //                            Units = (Byte?)GetDim(3),
    //                            Flat = (String)GetDim(4),
    //                            CenterX = (Int16?)GetDim(5),
    //                            CenterY = (Int16?)GetDim(6),
    //                            PositiveX = (String)GetDim(7),
    //                            PositiveY = (String)GetDim(8)
    //                        }; break;

    //                    case "Pir":
    //                        yield return new LinqToStdf.Records.V4.Pir
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1)
    //                        }; break;
    //                    case "Prr":
    //                        yield return new LinqToStdf.Records.V4.Prr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1),
    //                            PartFlag = (Byte)GetDim(2),
    //                            TestCount = (UInt16)GetDim(3),
    //                            HardBin = (UInt16)GetDim(4),
    //                            SoftBin = (UInt16?)GetDim(5),
    //                            XCoordinate = (Int16?)GetDim(6),
    //                            YCoordinate = (Int16?)GetDim(7),
    //                            TestTime = (UInt32?)GetDim(8),
    //                            PartId = (String)GetDim(9),
    //                            PartText = (String)GetDim(10),
    //                            PartFix = ToArray<Byte>(GetDim(11)),
    //                            SupersedesPartId = (Boolean)GetDim(12),
    //                            SupersedesCoords = (Boolean)GetDim(13),
    //                            AbnormalTest = (Boolean)GetDim(14),
    //                            Failed = (Boolean?)GetDim(15)
    //                        }; break;

    //                    case "Tsr":
    //                        yield return new LinqToStdf.Records.V4.Tsr
    //                        {
    //                            HeadNumber = (Byte?)GetDim(0),
    //                            SiteNumber = (Byte?)GetDim(1),
    //                            TestType = (String)GetDim(2),
    //                            TestNumber = (UInt32)GetDim(3),
    //                            ExecutedCount = (UInt32?)GetDim(4),
    //                            FailedCount = (UInt32?)GetDim(5),
    //                            AlarmCount = (UInt32?)GetDim(6),
    //                            TestName = (String)GetDim(7),
    //                            SequencerName = (String)GetDim(8),
    //                            TestLabel = (String)GetDim(9),
    //                            TestTime = (Single?)GetDim(10),
    //                            TestMin = (Single?)GetDim(11),
    //                            TestMax = (Single?)GetDim(12),
    //                            TestSum = (Single?)GetDim(13),
    //                            TestSumOfSquares = (Single?)GetDim(14)
    //                        }; break;

    //                    case "Ptr":
    //                        yield return new LinqToStdf.Records.V4.Ptr
    //                        {
    //                            TestNumber = (UInt32)GetDim(0),
    //                            HeadNumber = (Byte?)GetDim(1),
    //                            SiteNumber = (Byte?)GetDim(2),
    //                            TestFlags = (Byte)GetDim(3),
    //                            ParametricFlags = (Byte)GetDim(4),
    //                            Result = (Single?)GetDim(5),
    //                            TestText = (String)GetDim(6),
    //                            AlarmId = (String)GetDim(7),
    //                            OptionalFlags = (Byte?)GetDim(8),
    //                            ResultScalingExponent = (SByte?)GetDim(9),
    //                            LowLimitScalingExponent = (SByte?)GetDim(10),
    //                            HighLimitScalingExponent = (SByte?)GetDim(11),
    //                            LowLimit = (Single?)GetDim(12),
    //                            HighLimit = (Single?)GetDim(13),
    //                            Units = (String)GetDim(14),
    //                            ResultFormatString = (String)GetDim(15),
    //                            LowLimitFormatString = (String)GetDim(16),
    //                            HighLimitFormatString = (String)GetDim(17),
    //                            LowSpecLimit = (Single?)GetDim(18),
    //                            HighSpecLimit = (Single?)GetDim(19)
    //                        }; break;

    //                    case "Mpr":
    //                        yield return new LinqToStdf.Records.V4.Mpr
    //                        {
    //                            TestNumber = (UInt32)GetDim(0),
    //                            HeadNumber = (Byte?)GetDim(1),
    //                            SiteNumber = (Byte?)GetDim(2),
    //                            TestFlags = (Byte)GetDim(3),
    //                            ParametricFlags = (Byte)GetDim(4),
    //                            PinStates = ToArray<Byte>(GetDim(5)),
    //                            Results = ToArray<Single>(GetDim(6)),
    //                            TestText = (String)GetDim(7),
    //                            AlarmId = (String)GetDim(8),
    //                            OptionalFlags = (Byte?)GetDim(9),
    //                            ResultScalingExponent = (SByte?)GetDim(10),
    //                            LowLimitScalingExponent = (SByte?)GetDim(11),
    //                            HighLimitScalingExponent = (SByte?)GetDim(12),
    //                            LowLimit = (Single?)GetDim(13),
    //                            HighLimit = (Single?)GetDim(14),
    //                            StartingCondition = (Single?)GetDim(15),
    //                            ConditionIncrement = (Single?)GetDim(16),
    //                            PinIndexes = ToArray<UInt16>(GetDim(17)),
    //                            Units = (String)GetDim(18),
    //                            IncrementUnits = (String)GetDim(19),
    //                            ResultFormatString = (String)GetDim(20),
    //                            LowLimitFormatString = (String)GetDim(21),
    //                            HighLimitFormatString = (String)GetDim(22),
    //                            LowSpecLimit = (Single?)GetDim(23),
    //                            HighSpecLimit = (Single?)GetDim(24)
    //                        }; break;

    //                    case "Ftr":
    //                        yield return new LinqToStdf.Records.V4.Ftr
    //                        {
    //                            TestNumber = (UInt32)GetDim(0),
    //                            HeadNumber = (Byte?)GetDim(1),
    //                            SiteNumber = (Byte?)GetDim(2),
    //                            TestFlags = (Byte)GetDim(3),
    //                            CycleCount = (UInt32?)GetDim(4),
    //                            RelativeVectorAddress = (UInt32?)GetDim(5),
    //                            RepeatCount = (UInt32?)GetDim(6),
    //                            FailingPinCount = (UInt32?)GetDim(7),
    //                            XFailureAddress = (Int32?)GetDim(8),
    //                            YFailureAddress = (Int32?)GetDim(9),
    //                            VectorOffset = (Int16?)GetDim(10),
    //                            ReturnIndexes = ToArray<UInt16>(GetDim(11)),
    //                            ReturnStates = ToArray<Byte>(GetDim(12)),
    //                            ProgrammedIndexes = ToArray<UInt16>(GetDim(13)),
    //                            ProgrammedStates = ToArray<Byte>(GetDim(14)),
    //                            FailingPinBitfield = ToBitarray(GetDim(15)),
    //                            VectorName = (String)GetDim(16),
    //                            OpCode = (String)GetDim(17),
    //                            TestText = (String)GetDim(18),
    //                            AlarmId = (String)GetDim(19),
    //                            ProgrammedText = (String)GetDim(20),
    //                            ResultText = (String)GetDim(21),
    //                            PatternGeneratorNumber = (Byte?)GetDim(22),
    //                            SpinMap = ToBitarray(GetDim(23))
    //                        }; break;

    //                    case "Bps":
    //                        yield return new LinqToStdf.Records.V4.Bps { Name = (String)GetDim(0) }; break;

    //                    case "Eps":
    //                        yield return new LinqToStdf.Records.V4.Eps { }; break;

    //                    case "Dtr":
    //                        yield return new LinqToStdf.Records.V4.Dtr { Text = (String)GetDim(0) }; break;
    //                }
    //            }
    //        }
    //    }

    //    [FilePath]
    //    [DisplayName("File name")]
    //    [Description("Path to file to save. May contain tags: <DATE> expands to date, <VERDICT> expands to testplan verdict")]
    //    public MacroString FilePath { get; set; }

    //    public STDF_ResultListener()
    //    {
    //        FilePath = new MacroString() { Text = "results/<Date>-<Verdict>.stdf" };
    //    }

    //    #region ResultListener
    //    private List<LinqToStdf.StdfRecord> Records = null;
    //    private TestPlanRun curPlanRun = null;

    //    public override void OnResultPublished(Guid stepRunId, ResultTable result)
    //    {
    //        base.OnResultPublished(stepRunId, result);

    //        if (STDFUtility.IsSTDFType(result))
    //            Records.AddRange(STDFUtility.GetRecord(result));
    //        else
    //            Log.Debug("Got an Non-STDF Test Result!");
    //    }

    //    public override void OnTestPlanRunStart(TestPlanRun planRun)
    //    {
    //        base.OnTestPlanRunStart(planRun);
    //        curPlanRun = planRun;
    //        Records.Add(new Pir());
    //    }

    //    public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
    //    {
    //        base.OnTestPlanRunCompleted(planRun, logStream);
    //        Records.Add(new Prr());
    //    }

    //    public override void Open()
    //    {
    //        base.Open();
    //        Records = new List<LinqToStdf.StdfRecord>();

    //        Records.Add(new Far());
    //        Records.Add(new Mir());
    //    }

    //    public override void Close()
    //    {
    //        Records.Add(new Pcr());
    //        Records.Add(new Mrr());

    //        try
    //        {
    //            using (StdfFileWriter fw = new StdfFileWriter(FilePath.Expand(curPlanRun)))
    //            {
    //                fw.WriteRecords(Records);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Error(ex);
    //            throw ex;
    //        }
    //        base.Close();
    //    }
    //    #endregion

    //    public string DefaultExtension
    //    {
    //        get { return "stdf"; }
    //    }

    //    public TimeSpan GetAverageDuration(TestStepRun testStep, int averageCount)
    //    {
    //        return TimeSpan.Zero;
    //    }
    //}

    //#endregion

    //  #endregion

    public enum IQ_GetForMeasurement
    {
        [Description("Generic")]
        None = 0,
        [Description("EVM")]
        EVM,
        [Description("SEM")]
        SEM,
    }
}

