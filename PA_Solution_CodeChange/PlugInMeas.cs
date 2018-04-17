using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Ivi.Visa.Interop;
using Ivi.Driver.Interop;
using Agilent.AgM9391.Interop;
using Agilent.AgM938x.Interop;

// for m9000 temp debug purpose
using System.Threading;
//using Keysight.M9000;
//using Keysight.M9000.ComponentModel;
using Agilent.AgM90XA.Interop;
using Agilent.M9000.Interfaces;
using Agilent.AgM9018.Interop;
using Keysight.S8901A.Common;
using Agilent.AgMD2.Interop;

namespace Keysight.S8901A.Measurement
{

    public class PlugInMeasBase : PlugInBase
    {
        private string[] s8901a_license = { "S8901A-1FP", "S8901A-1FL", "S8901A-1TL", "S8901A-1TP" };
        private string[] s8902a_license = { "S8902A-1FP", "S8902A-1FL", "S8902A-1TL", "S8902A-1TP" };
        protected bool s8901a_licensed = true;
        protected bool s8902a_licensed = true;

        public PlugInMeasBase()
        {

        }
        #region License
        protected bool S890xA_LicenseInit()
        {
            bool ret = true;
            return ret;
            try
            {
                if (LicenseManager.CreateResult != LicensingCreateResult.SUCCESS) LicenseManager.Create();
                s8901a_licensed = LicenseManager.Instance.IsLicensed(s8901a_license);
                s8902a_licensed = LicenseManager.Instance.IsLicensed(s8902a_license);
            }
            catch (Exception ex)
            {
                LogFunc(LogLevel.ERROR, "License Server Connection Failed: " + LicenseManager.CreateResult.ToString());
                ret = false;
            }
            finally
            {
                LicenseManager.Finish();
            }

            return ret;
        }
        #endregion

        protected bool GetCableLoss(ref double dutInputLoss, ref double dutOutputLoss, double vsg_freq, double vsa_freq)
        {
            try
            {
                dutInputLoss = Utility.getInputAtten(vsg_freq);
                dutOutputLoss = Utility.getOutputAtten(vsa_freq);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR: Get Input/Output Cable Loss Error!");
            }
        }
        protected delegate void LF(LogLevel lvl, string s, params object[] obj);
        protected Func<Dictionary<TestItem_Enum, TestPoint>, TestItem_Enum, double, double, LF, bool> setAndCheckResult = (tps, key, result, testtime, log) =>  
        {
            bool pass = true;
            TestPoint tp;
            if (tps.TryGetValue(key, out tp))
            {
                tp.TestResult = result;
                tp.TestTime = testtime;
                if ((tp.TestResult <= tp.LimitHigh) && (tp.TestResult >= tp.LimitLow))
                {
                    pass = true;
                    log(LogLevel.INFO, "Passed due to {0}({1}) is in range ({2},{3})", key, result, tp.LimitLow, tp.LimitHigh);
                }
                else
                {
                    pass = false;
                    log(LogLevel.ERROR, "Failed due to {0}({1}) not in range ({2},{3})", key, result, tp.LimitLow, tp.LimitHigh);
                }
            }
            return pass;
        };

    }

    public class PlugInMeasCommon : PlugInMeasBase
    {
        public IDIO dio { set; get; }
        public IPPMU ppmu { set; get; }
        public IDCSMU[] dcsmus;

        /// <summary>
        /// Constructor for PlugInMeasCommon Class
        /// </summary>
        /// <param name="idio">digital IO interface <see cref="IDIO"/> </param>
        /// <param name="idcsmus">source measure unit interface <see cref="IDCSMU"/>[]</param>
        public PlugInMeasCommon(IDIO idio, IDCSMU[] idcsmus)
        {
            S890xA_LicenseInit();
            if (s8901a_licensed == false)
            {
                LogFunc(LogLevel.ERROR, "S8901A license not installed");
                return;
            }
            dio = idio;
            dcsmus = idcsmus;
        }

        /// <summary>
        /// Function to setup digital IO state through RFFE interface
        /// </summary>
        /// <param name="set_vio">whether set VIO or not</param>
        /// <param name="vio_state">the <see cref="VIO_STATE"/> to be set</param>
        /// <param name="vio_delay">the delay after setting VIO state and before sending RFFE command, in milisecond</param>
        /// <param name="filename">the RFFE command file used to be send to digital IO</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Dio(bool set_vio, VIO_STATE vio_state, int vio_delay, string filename)
        {
            bool ret = true;
            try
            {
                if (set_vio)
                {
                    ret = dio.setVioState(vio_state == VIO_STATE.VIO_STATE_OFF ? false : true);
                    System.Threading.Thread.Sleep(vio_delay);
                    Logging(LogLevel.INFO, "DIO Set VIO State ret {0}, delay {1} ms.", ret, vio_delay);
                }
                ret = ret && dio.readRffeCommands(filename);
                Logging(LogLevel.INFO, "DIO readRffeCommands ret {0}", ret);
                ret = ret && dio.sendRffeCommands();
                Logging(LogLevel.INFO, "DIO sendRffeCommands ret {0}", ret);

                if (set_vio && vio_state == VIO_STATE.VIO_STATE_ON)
                    ret = ret && _Dio_CheckRegister();
            }
            catch
            {
                ret = false;
            }

            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Dio failed!");

            return ret;
        }

        /// <summary>
        /// Function to setup digital IO state through RFFE interface
        /// </summary>
        /// <param name="set_vio">whether set VIO or not</param>
        /// <param name="vio_state">the <see cref="VIO_STATE"/> to be set</param>
        /// <param name="vio_delay">the delay after setting VIO state and before sending RFFE command, in milisecond</param>
        /// <param name="dioinfo">the RFFE commands saved in <see cref="DIOInfo"/>[]</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Dio(bool set_vio, VIO_STATE vio_state, int vio_delay, DIOInfo[] dioinfo)
        {
            bool ret = true;

            try
            {
                if (set_vio)
                {
                    dio.setVioState(vio_state == VIO_STATE.VIO_STATE_OFF ? false : true);
                    System.Threading.Thread.Sleep(vio_delay);
                    Logging(LogLevel.INFO, "DIO Set VIO State ret {0}, delay {1} ms.", ret, vio_delay);
                }
                dio.info = dioinfo;
                ret = ret && dio.sendRffeCommands();
                Logging(LogLevel.INFO, "DIO sendRffeCommands ret {0}", ret);

                if (set_vio && vio_state == VIO_STATE.VIO_STATE_ON)
                    ret = ret && _Dio_CheckRegister();
            }
            catch
            {
                ret = false;
            }


            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Dio failed!");

            return ret;
        }

        private bool _Dio_CheckRegister()
        {
            bool ret = true;

            for (int i = 0; i < dio.info.Length; i++)
            {
                if (dio.info[i].rffeRegCheck)
                {
                    if (dio.info[i].rffeReadData != dio.info[i].rffeExpectedValue)
                    {
                        ret = false;
                        Logging(LogLevel.INFO, "Register expected value is {0}, but readback value is {1}, for command[{2}], Address is {3}, Register is {4}",
                            dio.info[i].rffeExpectedValue, dio.info[i].rffeReadData, i,
                            dio.info[i].rffeAddress, dio.info[i].rffeRegister);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Function to setup digital IO through I2C interface
        /// </summary>
        /// <param name="filename">the I2C command file</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Dio_I2C(string filename)
        {
            bool ret = true;
            try
            {
                ret = ret && dio.I2CreadCommands(filename);
                Logging(LogLevel.INFO, "DIO I2CreadCommands ret {0}", ret);
                ret = ret && dio.I2CsendCommands();
                Logging(LogLevel.INFO, "DIO I2CsendCommands ret {0}", ret);
                ret = ret && _Dio_CheckRegister();
            }
            catch
            {
                ret = false;
            }

            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Dio_I2C failed!");

            return ret;
        }

        /// <summary>
        /// Function to setup digital IO through I2C interface
        /// </summary>
        /// <param name="dioinfo">the <see cref="DIOInfo"/>[] </param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Dio_I2C(DIOInfo[] dioinfo)
        {
            bool ret = true;

            try
            {
                dio.info = dioinfo;
                ret = ret && dio.I2CsendCommands();
                Logging(LogLevel.INFO, "DIO I2CsendCommands ret {0}", ret);
                ret = ret && _Dio_CheckRegister();
            }
            catch
            {
                ret = false;
            }


            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Dio_I2C failed!");

            return ret;
        }

        /// <summary>
        /// Function to perform PPMU measurement
        /// </summary>
        /// <param name="chanName">the name of the channel defined in PPMU configuration file</param>
        /// <param name="operation">the <see cref="PPMU_OPERATION"/> to be performed</param>
        /// <param name="level">the forced voltage/current depend on the <see cref="PPMU_OPERATION"/></param>
        /// <param name="tps">the test item <see cref="Dictionary{TestItem_Enum, TestPoint}"/> definition)</param>
        /// <param name="r">the measurement result depends on the <see cref="PPMU_OPERATION"/> </param>
        /// <param name="limit">the upper limit for the test result</param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasurePPMU(string chanName, PPMU_OPERATION operation, double level, Dictionary<TestItem_Enum, TestPoint> tps, ref double r, double limit = 0.04)
        {
            ppmu = dio as IPPMU;
            if (ppmu == null) return false;

            bool ret = true;
            double[] result = null;
            TestPoint t;
            Stopwatch sw = new Stopwatch();
            sw.Reset();

            switch (operation)
            {
                case PPMU_OPERATION.FORCE_CURRENT:
                    ppmu.ppmuFc(chanName, level);
                    ret = true;
                    break;

                case PPMU_OPERATION.FORCE_VOLTAGE:
                    ppmu.ppmuFv(chanName, level);
                    ret = true;
                    break;

                case PPMU_OPERATION.FORCE_NOTHING_MEASURE_VOLTAGE:
                    sw.Start();
                    result = ppmu.ppmuFnmv(chanName);
                    sw.Stop();
                    t = tps.FirstOrDefault(c => c.Key == TestItem_Enum.PPMU_VOLT).Value;
                    t.TestResult = result[0];
                    t.TestTime = sw.ElapsedMilliseconds;
                    ret = result[0] < t.LimitHigh && result[0] >= t.LimitLow;
                    break;

                case PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_VOLTAGE:
                    sw.Start();
                    result = ppmu.ppmuFvmv(chanName, level);
                    sw.Stop();
                    t = tps.FirstOrDefault(c => c.Key == TestItem_Enum.PPMU_VOLT).Value;
                    t.TestResult = result[0];
                    t.TestTime = sw.ElapsedMilliseconds;
                    ret = result[0] < t.LimitHigh && result[0] >= t.LimitLow;
                    break;

                case PPMU_OPERATION.FORCE_VOLTAGE_MEASURE_CURRENT:
                    sw.Start();
                    result = ppmu.ppmuFvmc(chanName, level, limit);
                    sw.Stop();
                    t = tps.FirstOrDefault(c => c.Key == TestItem_Enum.PPMU_CURR).Value;
                    t.TestResult = result[0];
                    t.TestTime = sw.ElapsedMilliseconds;
                    ret = result[0] < t.LimitHigh && result[0] >= t.LimitLow;
                    break;

                case PPMU_OPERATION.FORCE_CURRENT_MEASURE_VOLTAGE:
                    sw.Start();
                    result = ppmu.ppmuFcmv(chanName, level);
                    sw.Stop();
                    t = tps.FirstOrDefault(c => c.Key == TestItem_Enum.PPMU_VOLT).Value;
                    t.TestResult = result[0];
                    t.TestTime = sw.ElapsedMilliseconds;
                    ret = result[0] < t.LimitHigh && result[0] >= t.LimitLow;
                    break;

                case PPMU_OPERATION.FORCE_CURRENT_MEASURE_CURRENT:
                    sw.Start();
                    result = ppmu.ppmuFcmc(chanName, level);
                    sw.Stop();
                    t = tps.FirstOrDefault(c => c.Key == TestItem_Enum.PPMU_CURR).Value;
                    t.TestResult = result[0];
                    t.TestTime = sw.ElapsedMilliseconds;
                    ret = result[0] < t.LimitHigh && result[0] >= t.LimitLow;
                    break;

                case PPMU_OPERATION.HIGH_IMPEDANCE:
                    sw.Start();
                    ppmu.ppmuHiZ(chanName);
                    sw.Stop();
                    ret = true;
                    break;

                default:
                    ret = false;
                    break;
            }

            if (result != null) r = result[0];
            else r = double.NaN;

            return ret;
        }

        /// <summary>
        /// Function to setup Source Measure Unit
        /// </summary>
        /// <param name="logicalChan">the logical SMU channel to be set</param>
        /// <param name="enabled">whether this channel is enabled</param>
        /// <param name="chantype">the <see cref="DCSMUInfo.SMU_CHANTYPE"/>for this channel</param>
        /// <param name="volt">the voltage to be set</param>
        /// <param name="current">the current to be set</param>
        /// <param name="meas_current">whether to <see cref="MeasureCurrent(DCSMUInfo.SMU_CHANTYPE, double, Dictionary{TestItem_Enum, TestPoint})"/> for this channel</param>
        /// <param name="meas_dc">whether chis channel is included in <see cref="PlugInMeas.MeasurePAE(bool, double, double, double, double, bool, int, Dictionary{TestItem_Enum, TestPoint}, bool, SECONDARY_SOURCE_ANALYZER_CONFIG)"/> measurement</param>
        /// <param name="triggerSource">the PXI trigger to be used</param>
        /// <param name="triggerOffset">the offset from the trigger, in points</param>
        /// <param name="iMeasurementDuration">the measurement duration</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Dcsmu(int logicalChan, bool enabled, DCSMUInfo.SMU_CHANTYPE chantype, double volt, double current, bool meas_current, bool meas_dc, DCSMUInfo.DCSMUTriggerSource triggerSource, int triggerOffset, int iMeasurementDuration)
        {
            bool found = false;
            bool ret = true;

            try
            {
                foreach (IDCSMU dcsmu in dcsmus)
                {
                    for (int i = 0; i < dcsmu.GetNumChans(); i++)
                    {
                        if (dcsmu.dcSmuInfo[i].logicalChanIdx == logicalChan)
                        {
                            dcsmu.dcSmuInfo[i].bEnabled = enabled;
                            dcsmu.dcSmuInfo[i].dVoltSetup = volt;
                            dcsmu.dcSmuInfo[i].dCurrentSetup = current;
                            dcsmu.dcSmuInfo[i].bMeasureCurrent = meas_current;
                            dcsmu.dcSmuInfo[i].bMeasureDC = meas_dc;
                            dcsmu.dcSmuInfo[i].chanType = chantype;
                            dcsmu.dcSmuInfo[i].triggerSource = triggerSource;
                            dcsmu.dcSmuInfo[i].triggerOffset = triggerOffset;
                            dcsmu.dcSmuInfo[i].MeasurementDuration = iMeasurementDuration;
                            dcsmu.SetupSmus();
                            if (enabled)
                                ret = dcsmu.CloseSmuOutputRelays();
                            else
                                ret = dcsmu.OpenSmuOutputRelays();
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_Dcsmu failed, {0}", ex.Message);
                ret = false;
            }

            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Dcsmu failed!");

            return ret;
        }

        /// <summary>
        /// Function to generate particular digital pattern
        /// </summary>
        /// <param name="eFEM_State_Transition">the transition(<see cref="FEM_STATE_TRANSITION"/> defined in STIL file) to be performed</param>
        /// <returns>true for success, false for fail</returns>
        public bool DIOPatternGen(FEM_STATE_TRANSITION eFEM_State_Transition)
        {
            //ppmu = dio as IPPMU;
            if (dio == null) return false;

            bool ret = true;
            double[] result = null;
            TestPoint t;
            Stopwatch sw = new Stopwatch();
            sw.Reset();

            dio.FEMStateTrans(eFEM_State_Transition);

            //switch (operation)
            //{
            //    case PPMU_OPERATION.FORCE_CURRENT:
            //        ppmu.ppmuFc(chanName, level);
            //        ret = true;
            //        break;

            //    default:
            //        ret = false;
            //        break;
            //}



            return ret;
        }

        /// <summary>
        /// Measure the DC Current, following parameters can be measured at the same time
        ///   - ICC, IBAT, ITOTAL
        /// </summary>
        /// <param name="chantype">the <see cref="DCSMUInfo.SMU_CHANTYPE"/>for this channel</param>
        /// <param name="tps">the test item <see cref="Dictionary{TestItem_Enum, TestPoint}"/> definition)</param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureCurrent(DCSMUInfo.SMU_CHANTYPE chanType, double range, Dictionary<TestItem_Enum, TestPoint> tps)
        {
            bool ret = true;
            double icc = 0;
            double ibat = 0;
            double icustom1 = 0;
            double icustom2 = 0;
            double itotal = 0;
            double current = 0;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                foreach (IDCSMU dcsmu in dcsmus)
                {
                    for (int i = 0; i < dcsmu.GetNumChans(); i++)
                    {
                        if (dcsmu.dcSmuInfo[i] != null &&
                            dcsmu.dcSmuInfo[i].bEnabled &&
                            dcsmu.dcSmuInfo[i].bMeasureCurrent)
                        {
                            dcsmu.MeasCurrent(dcsmu.dcSmuInfo[i].logicalChanIdx, ref current, range);

                            if (dcsmu.dcSmuInfo[i].chanType == DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VCC)
                            {
                                icc += current;
                            }
                            if (dcsmu.dcSmuInfo[i].chanType == DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VBAT)
                            {
                                ibat += current;
                            }
                            if (dcsmu.dcSmuInfo[i].chanType == DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_1)
                            {
                                icustom1 += current;
                            }
                            if (dcsmu.dcSmuInfo[i].chanType == DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_2)
                            {
                                icustom2 += current;
                            }
                            Logging(LogLevel.INFO, "LogicalChannel {0}:  Current:{1} ", dcsmu.dcSmuInfo[i].logicalChanIdx, current);
                        }
                    }
                }
                itotal = icc + ibat + icustom1 + icustom2;
                sw.Stop();

                ret = setAndCheckResult(tps, TestItem_Enum.ICC, icc, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.IBAT, ibat, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.ICUSTOM1, icustom1, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.ICUSTOM2, icustom1, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.ITOTAL, itotal, sw.ElapsedMilliseconds, Logging) && ret;

                return ret;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureLeakageCurrent Error: {0}", ex.Message);
                ret = false;
            }

            Logging(LogLevel.INFO, "Leakage Current Measurement return {0}", ret);
            return true;
        }

    }

    public class PlugInMeas : PlugInMeasBase
    {
        #region Private Section

        private IVSG vsg1;
        private IVSA vsa1;
        private IVXT vxt1;
        private IVSG vsg2;
        private IVSA vsa2;
        private IVXT vxt2;

        private IVNA vna;
        private IAWG awg;
        public IVSA cxa_m;
        private PlugInMeasCommon measCommon;

        public TestParameters INIParam { get; set; }
        private ResourceManager oRm;
        private FormattedIO488 pm;

        private PlugInVsa89600 vsa89600 = null;
        private delegate void LF(LogLevel lvl, string s, params object[] obj);
        private Func<Dictionary<TestItem_Enum, TestPoint>, TestItem_Enum, double, double, LF, bool> setAndCheckResult = (tps, key, result, testtime, log) =>
        {
            bool pass = true;
            TestPoint tp;
            if (tps.TryGetValue(key, out tp))
            {
                tp.TestResult = result;
                tp.TestTime = testtime;
                if ((tp.TestResult <= tp.LimitHigh) && (tp.TestResult >= tp.LimitLow))
                {
                    pass = true;
                    log(LogLevel.INFO, "Passed due to {0}({1}) is in range ({2},{3})", key, result, tp.LimitLow, tp.LimitHigh);
                }
                else
                {
                    pass = false;
                    log(LogLevel.ERROR, "Failed due to {0}({1}) not in range ({2},{3})", key, result, tp.LimitLow, tp.LimitHigh);
                }
            }
            return pass;
        };
        private Func<Dictionary<TestItem_Enum, TestPoint>, TestItem_Enum, double[], double, LF, bool> setAndCheckEvmResults = (tps, key, result, testtime, log) =>
        {
            bool pass = true;
            TestPoint tp;
            if (tps.TryGetValue(key, out tp))
            {
                tp.ComponentCarrierEvm = new double[result.Count()];
                tp.TestTime = testtime;

                for (int i =0; i < result.Count(); i++)
                {
                    tp.ComponentCarrierEvm[i] = result[i];
                    if ((tp.ComponentCarrierEvm[i] <= tp.LimitHigh) && (tp.ComponentCarrierEvm[i] >= tp.LimitLow))
                    {
                        log(LogLevel.INFO, "Carrier {0} Passed due to {1}({2}) is in range ({3},{4})", i, key, result[i], tp.LimitLow, tp.LimitHigh);
                    }
                    else
                    {
                        pass = false;
                        log(LogLevel.INFO, "Carrier {0} Failed due to {1}({2}) is not in range ({3},{4})", i, key, result[i], tp.LimitLow, tp.LimitHigh);
                    }
                }
            
            }
            return pass;
        };


        private bool xAppLoaded = false;

        private PlugInKMF plugInKmf = null;
        private PlugInXApp plugInXapp = null;
        private PlugInVsaXapp plugInVsaXapp = null;

        public void setup2ndSourceAnalyzer(ref IVSG local_vsg, ref IVSA local_vsa, ref IVXT local_vxt,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            local_vsg = vsg1;
            local_vsa = vsa1;
            local_vxt = vxt1;

            if (use_secondary_source_analyzer)
            {
                if (secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.NONE)
                {
                    local_vsg = null;
                    local_vsa = null;
                    local_vxt = null;
                }
                else if (secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
                {
                    local_vsa = vsa2;
                    local_vxt = null;
                }
                /*
                else if (secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.VSG_ONLY)
                {
                    local_vsg = vsg2;
                }
                else if (secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.VSG_AND_VSA)
                {
                    local_vsg = vsg2;
                    local_vsa = vsa2;
                }
                else if (secondary_source_analyzer_config == SECONDARY_SOURCE_ANALYZER_CONFIG.VXT)
                {
                    local_vxt = vxt2;
                    local_vsg = local_vxt as IVSG;
                    local_vsa = local_vxt as IVSA;
                }
                */
            }
        }

        private bool genPulseSequenceAndPlay(IVSG vsg, IVSA vsa, string refName, double leadTime, double dutyCycle, bool isDynamicOperation)
        {
            bool ret = true;
            ArbInfo a = new ArbInfo();

            ret = ret && vsg.GetArbInfo(refName, ref a);
            vsg.Arb = a;

            ret = vsg.StopModulation();

            if (isDynamicOperation)
            {
                ret = ret && vsg.GeneratePulseArbSequenceForDynamicEVM(refName, leadTime, dutyCycle);
            }
            else
            {
                ret = ret && vsg.GeneratePulseArbSequence(refName, leadTime, dutyCycle);
            }

            // REVISIT-Jianhui-20170809:
            // We found an issue that in M9391A, following IVI function call sequence result in failure.
            // So do _not_ apply for the moment since the Source/Analyzer setup or measurement steps will
            // do VSA.Apply() anyway. But we need to check this with M9391A/M9393A IVI team to understand
            // the root cause of this issue.
            if (vsa != null)
            ret = ret && vsa.SetRms(a.arbRmsValue, false);

            ret = ret && vsg.PlaySequence(refName);

            return ret;
        }

        #endregion

        #region Measurement Parameter
        public double servoHeadRoom = 3.0;
        public const string CW_ARB = "CW ARB";
        public const string POWER_RAMP_ARB = "Power Ramp ARB";
        public string cw_two_tone_waveform = "PA_TwoTone.wfm";
        public string cw_modulation_waveform = "PA_GF_IM3_CW_LTE_5MHz.wfm";
        public bool xAppLoadStatus
        {
            get { return xAppLoaded; }
            set { xAppLoaded = value; }
        }
        #endregion

        #region Constructor/Destructor
        /// <summary>
        /// The Constructor for PlugInMeas
        /// </summary>
        /// <param name="ivsg">the primary <see cref="IVSG"/> interface</param>
        /// <param name="ivsa">the primary <see cref="IVSA"/> interface</param>
        /// <param name="measComm">the <see cref="PlugInMeasCommon"/> object</param>
        /// <param name="ivna">the <see cref="IVNA"/> interface </param>
        /// <param name="iawg">the <see cref="IAWG"/> interface </param>
        /// <param name="kmf">the <see cref="PlugInKMF"/> object</param>
        /// <param name="use_secondary_source_analyzer">whether to use 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/></param>
        /// <param name="ivsg_2">the secondary <see cref="IVSG"/> interface </param>
        /// <param name="ivsa_2">the secondary <see cref="IVSA"/> interface </param>
        /// <param name="ivxt_2">the secondary <see cref="IVXT"/> interface </param>
        public PlugInMeas(IVSG ivsg, IVSA ivsa, PlugInMeasCommon measComm, IVNA ivna, IAWG iawg, PlugInKMF kmf,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
            IVSG ivsg_2 = null, IVSA ivsa_2 = null, IVXT ivxt_2 = null)
        {
            S890xA_LicenseInit();
            if (s8901a_licensed == false)
            {
                LogFunc(LogLevel.ERROR, "S8901A license not installed");
                return;
            }
            INIParam = new TestParameters();
            vsg1 = ivsg;
            vsa1 = ivsa;
            measCommon = measComm;
            vna = ivna;
            awg = iawg;
            vxt1 = null;
            if (vsg1 == null || vsa1 == null)
            {
                //throw (new ArgumentNullException());
            }

            vsg2 = ivsg_2;
            vsa2 = ivsa_2;
            vxt2 = ivxt_2;

            plugInKmf = kmf;
        }

        /// <summary>
        /// The Constructor for PlugInMeas
        /// </summary>
        /// <param name="trx">the primary <see cref="IVXT"/> interface</param>
        /// <param name="measComm">the <see cref="PlugInMeasCommon"/> object</param>
        /// <param name="ivna">the <see cref="IVNA"/> interface </param>
        /// <param name="iawg">the <see cref="IAWG"/> interface </param>
        /// <param name="kmf">the <see cref="PlugInKMF"/> object</param>
        /// <param name="use_secondary_source_analyzer">whether to use 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/></param>
        /// <param name="ivsg_2">the secondary <see cref="IVSG"/> interface </param>
        /// <param name="ivsa_2">the secondary <see cref="IVSA"/> interface </param>
        /// <param name="ivxt_2">the secondary <see cref="IVXT"/> interface </param>
        public PlugInMeas(IVXT trx, PlugInMeasCommon measComm, IVNA ivna, IAWG iawg, PlugInKMF kmf,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
            IVSG ivsg_2 = null, IVSA ivsa_2 = null, IVXT ivxt_2 = null)
        {
            S890xA_LicenseInit();
            if (s8901a_licensed == false)
            {
                LogFunc(LogLevel.ERROR, "S8901A license not installed");
                return;
            }

            INIParam = new TestParameters();
            vxt1 = trx;
            vsa1 = vxt1 as IVSA;
            vsg1 = vxt1 as IVSG;
            measCommon = measComm;
            vna = ivna;
            awg = iawg;
            if (vxt1 == null)
            {
                //throw (new ArgumentNullException());
            }

            vsg2 = ivsg_2;
            vsa2 = ivsa_2;
            vxt2 = ivxt_2;

            plugInKmf = kmf;
        }

        public void SetXApp(PlugInXApp xapp)
        {
            plugInXapp = xapp;
        }

        public void SetVsaXapp(PlugInVsaXapp vsaxapp)
        {
            plugInVsaXapp = vsaxapp;
            if (vsaxapp != null)
                plugInXapp = vsaxapp.plugInXapp;
        }

        public void SetVsa89600(PlugInVsa89600 vsa)
        {
            vsa89600 = vsa;
        }
        #endregion


        #region Test Condition Setup
        //public bool TestConditionSetup_LoadArb(string filePath, string fileName)
        //{
        //    bool ret = true;
        //    ret = vsg.LoadARB(filePath, fileName, 0, 0);
        //    Console.WriteLine("LoadArb and Play return {0}", ret);
        //    return ret;
        //}
        //public bool TestConditionSetup_PlayArb(ref double arb_rms, string arb = null)
        //{
        //    bool ret = true;
        //    ArbInfo a = new ArbInfo();
        //    if (arb != null)
        //    {
        //        ret = vsg.PlayARB(arb);
        //        vsg.GetArbInfo(arb, ref a);
        //    }
        //    else
        //    {
        //        vsg.GetARBCatalog();
        //        string w = vsg.WaveformList[0];
        //        ret = vsg.PlayARB(w);
        //        vsg.GetArbInfo(w, ref a);
        //    }
        //    arb_rms = a.arbRmsValue;

        //    Console.WriteLine("PlayArb() ret {0}, Arb RMS: {1}", ret, arb_rms);
        //    return ret;
        //}
        //public bool TestConditionSetup_LoadINI(SUPPORTEDTECHNOLOGIES tech, string inifile, string waveformPath)
        //{
        //    bool ret = true;
        //    ret = vsa.LoadINITestInfo(tech, inifile, waveformPath);
        //    Console.WriteLine("LoadINI {0}", ret);
        //    return ret;
        //}

        /// <summary>
        /// Function to fetch the waveform information for the selected technology
        /// </summary>
        /// <param name="selected_technology">the selected technology</param>
        /// <param name="tech_format">the <see cref="SUPPORTEDFORMAT"/> of this waveform </param>
        /// <param name="bw">the bandwidth of this waveform</param>
        /// <param name="modtype">the <see cref="MODULATIONTYPE"/> of this waveform </param>
        /// <param name="dir">the <see cref="DIRECTION"/> of this waveform </param>
        public void TestConditionSetup_GetWaveformInfo(string selected_technology, ref SUPPORTEDFORMAT tech_format, ref double bw, ref MODULATIONTYPE modtype, ref DIRECTION dir)
        {
            var tp = from r in INIParam.testParams
                     where (r.technology.Equals(selected_technology))
                     select r;

            #region Format Mapping
            switch (tp.FirstOrDefault().Format)
            {
                case "CW": tech_format = SUPPORTEDFORMAT.CW; break;
                case "GSM": tech_format = SUPPORTEDFORMAT.GSM; break;
                case "EDGE": tech_format = SUPPORTEDFORMAT.EDGE; break;
                case "EVDO": tech_format = SUPPORTEDFORMAT.EVDO; break;
                case "WCDMA": tech_format = SUPPORTEDFORMAT.WCDMA; break;
                case "CDMA2000": tech_format = SUPPORTEDFORMAT.CDMA2000; break;
                default:
                case "LTEFDD": tech_format = SUPPORTEDFORMAT.LTEFDD; break;
                case "LTETDD": tech_format = SUPPORTEDFORMAT.LTETDD; break;
                case "LTEAFDD": tech_format = SUPPORTEDFORMAT.LTEAFDD; break;
                case "LTEATDD": tech_format = SUPPORTEDFORMAT.LTEATDD; break;
                case "TDSCDMA": tech_format = SUPPORTEDFORMAT.TDSCDMA; break;
                case "WLAN11N": tech_format = SUPPORTEDFORMAT.WLAN11N; break;
                case "WLAN11AC": tech_format = SUPPORTEDFORMAT.WLAN11AC; break;
                case "WLAN11AX": tech_format = SUPPORTEDFORMAT.WLAN11AX; break;
                case "WLAN11A": tech_format = SUPPORTEDFORMAT.WLAN11A; break;
                case "WLAN11B": tech_format = SUPPORTEDFORMAT.WLAN11B; break;
                case "WLAN11G": tech_format = SUPPORTEDFORMAT.WLAN11G; break;
                case "WLAN11J": tech_format = SUPPORTEDFORMAT.WLAN11J; break;
                case "WLAN11P": tech_format = SUPPORTEDFORMAT.WLAN11P; break;
                case "5GNR": tech_format = SUPPORTEDFORMAT._5GNR; break;
            }
            #endregion

            #region BandWidth Mapping
            var s = tp.FirstOrDefault().Bandwidth;
            if (s.EndsWith("MHz", true, null))
            {
                bw = Convert.ToDouble((tp.FirstOrDefault().Bandwidth.Substring(0, s.Length - 3))) * 1e6;
            }
            else
            {
                bw = 5.0e6;
                Logging(LogLevel.WARNING, "Bandwidth format invalid! Use 5MHz bandwidth.");
            }
            #endregion

            #region Modulation Type Mapping
            switch (tp.FirstOrDefault().Modulation)
            {
                case "BPSK": modtype = MODULATIONTYPE.BPSK; break;
                case "QPSK": modtype = MODULATIONTYPE.QPSK; break;
                default:
                case "QAM16": modtype = MODULATIONTYPE.QAM16; break;
                case "QAM64": modtype = MODULATIONTYPE.QAM64; break;
                case "QAM256": modtype = MODULATIONTYPE.QAM256; break;
                case "QAM1024": modtype = MODULATIONTYPE.QAM1024; break;
            }

            #endregion

            #region Direction Mapping
            switch (tp.FirstOrDefault().Direction)
            {
                default:
                case "UPLINK":
                    dir = DIRECTION.UPLINK;
                    break;
                case "DOWNLINK":
                    dir = DIRECTION.DOWNLINK;
                    break;
            }

            #endregion
        }

        /// <summary>
        /// Function to get the format of the selected technology
        /// </summary>
        /// <param name="selected_technology">the selected technology</param>
        /// <returns>the <see cref="SUPPORTEDFORMAT"/> of this waveform </returns>
        public SUPPORTEDFORMAT TestConditionSetup_GetFormat(string selected_technology)
        {
            var tp = from r in INIParam.testParams
                     where (r.technology.Equals(selected_technology))
                     select r;
            SUPPORTEDFORMAT tech_format;

            #region Format Mapping
            if (selected_technology != "" && tp != null && tp.FirstOrDefault() != null)
            {
                switch (tp.FirstOrDefault().Format)
                {
                    case "CW": tech_format = SUPPORTEDFORMAT.CW; break;
                    case "GSM": tech_format = SUPPORTEDFORMAT.GSM; break;
                    case "EDGE": tech_format = SUPPORTEDFORMAT.EDGE; break;
                    case "EVDO": tech_format = SUPPORTEDFORMAT.EVDO; break;
                    case "WCDMA": tech_format = SUPPORTEDFORMAT.WCDMA; break;
                    case "CDMA2000": tech_format = SUPPORTEDFORMAT.CDMA2000; break;
                    default:
                    case "LTEFDD": tech_format = SUPPORTEDFORMAT.LTEFDD; break;
                    case "LTETDD": tech_format = SUPPORTEDFORMAT.LTETDD; break;
                    case "LTEAFDD": tech_format = SUPPORTEDFORMAT.LTEAFDD; break;
                    case "LTEATDD": tech_format = SUPPORTEDFORMAT.LTEATDD; break;
                    case "TDSCDMA": tech_format = SUPPORTEDFORMAT.TDSCDMA; break;
                    case "WLAN11N": tech_format = SUPPORTEDFORMAT.WLAN11N; break;
                    case "WLAN11AC": tech_format = SUPPORTEDFORMAT.WLAN11AC; break;
                    case "WLAN11AX": tech_format = SUPPORTEDFORMAT.WLAN11AX; break;
                    case "WLAN11A": tech_format = SUPPORTEDFORMAT.WLAN11A; break;
                    case "WLAN11B": tech_format = SUPPORTEDFORMAT.WLAN11B; break;
                    case "WLAN11G": tech_format = SUPPORTEDFORMAT.WLAN11G; break;
                    case "WLAN11J": tech_format = SUPPORTEDFORMAT.WLAN11J; break;
                    case "WLAN11P": tech_format = SUPPORTEDFORMAT.WLAN11P; break;
                    case "5GNR": tech_format = SUPPORTEDFORMAT._5GNR; break;
                }
            }
            else
            {
                tech_format = SUPPORTEDFORMAT.CW;
            }
            #endregion
            return tech_format;
        }

        /// <summary>
        /// Function to get the bandwidth of the selected technology
        /// </summary>
        /// <param name="selected_technology">the selected technology</param>
        /// <returns>the bandwidth of this waveform</returns>
        public double TestConditionSetup_GetBandWidth(string selected_technology)
        {
            var tp = from r in INIParam.testParams
                     where (r.technology.Equals(selected_technology))
                     select r;
            double bandwidth = 0.0;

            #region get bandwidth value
            if (selected_technology != "" && tp != null && tp.FirstOrDefault() != null)
            {
                var s = tp.FirstOrDefault().Bandwidth;
                if (s.EndsWith("MHz", true, null))
                {
                    bandwidth = Convert.ToDouble(s.Substring(0, s.Length-3)) * 1e6;
                }
                else
                    throw new InvalidDataException("Bandwidth format invalid!");
            }
            else
            {
                bandwidth = 5.0e6;
            }
            #endregion
            return bandwidth;
        }

        /// <summary>
        /// Function to get the carrier count of a selected technology.
        /// </summary>
        /// <param name="selected_technology">the selected technology</param>
        /// <returns>the carrier count of the waveform</returns>
        public int TestConditionSetup_GetCarrierCount(string selected_technology)
        {
            var tp = from r in INIParam.testParams
                     where (r.technology.Equals(selected_technology))
                     select r;
            int CarrierCount = 1;

            #region get bandwidth value
            if (selected_technology != "" && tp != null && tp.FirstOrDefault() != null&& (tp.FirstOrDefault().CarrierCount>=0&& tp.FirstOrDefault().CarrierCount<=10))
            {
                CarrierCount = System.Convert.ToInt32(tp.FirstOrDefault().CarrierCount);
            }
            else
            {
                CarrierCount = 1;
            }
            #endregion
            return CarrierCount;
        }

        /// <summary>
        /// Function to get the direction of a selected technology
        /// </summary>
        /// <param name="selected_technology">the selected technology</param>
        /// <returns>the <see cref="DIRECTION"/> of the waveform </returns>
        public DIRECTION TestConditionSetup_GetDirection(string selected_technology)
        {
            var tp = from r in INIParam.testParams
                     where (r.technology.Equals(selected_technology))
                     select r;
            string direction = "";

            #region Direction Mapping
            if (selected_technology != "" && tp != null && tp.FirstOrDefault() != null)
            {
                direction = tp.FirstOrDefault().Direction;
            }
            else
            {
                direction = "UPLINK";
            }
            #endregion

            if (direction.Equals("UPLINK"))
                return DIRECTION.UPLINK;
            else
                return DIRECTION.DOWNLINK;
        }

        /// <summary>
        /// Function to load the waveforms into Signal Generator, at the same time,
        /// initialize all the test parameter for those loaded Arbs.
        /// </summary>
        /// <param name="iniFile">the INI system configuration file</param>
        /// <param name="waveformPath">the waveform path</param>
        /// <param name="reload_waveform">whether to reload the arbs</param>
        /// <param name="use_secondary_source_analyzer">whether to use the secondary source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the secondary source/analyzer configuration <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_LoadTestParameters(string iniFile, string waveformPath,
            bool reload_waveform = true,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                if (reload_waveform)
                {
                    // Load INI Test Parameters
                    ret = INIParam.LoadINITestParams(iniFile, waveformPath);
                    if (!ret)
                    {
                        throw new Exception("LoadINITestParams Failed!");
                    }

                    if (vsg != null && vsg.Initialized())
                    {
                        // Load All Arbs defined in INI File
                        for (int i = 0; i < INIParam.testParams.Count; i++)
                        {
                            string s = INIParam.testParams[i].waveformName;
                            if (s != null && s != string.Empty && INIParam.testParams[i].technology.Equals("CW") == false)
                            {
                                ret = vsg.LoadARB(waveformPath, s);
                                if (!ret)
                                {
                                    throw new Exception("VSG LoadARB Failed!");
                                }
                                ArbInfo ai = new ArbInfo();
                                vsg.GetArbInfo(s, ref ai);
                                INIParam.testParams[i].waveformLength = ai.numSamples / ai.arbSampleRate;
                            }
                            else if (INIParam.testParams[i].technology.Equals("CW"))
                            {
                                if (vxt == null)
                                {
                                    ret = vsg.GeneratePowerRampArb(CW_ARB, 0, 0, 1000e-6, 4e6);
                                    if (!ret)
                                    {
                                        throw new Exception("VSG GeneratePowerRampArb (CW_ARB) Failed!");
                                    }

                                    ret = vsg.GeneratePowerRampArb(POWER_RAMP_ARB, -30, 0, 100e-6, 4e6);
                                    if (!ret)
                                    {
                                        throw new Exception("VSG GeneratePowerRampArb (POWER_RAMP_ARB) Failed!");
                                    }
                                }
                                else
                                {
                                    // NOTE: VXT's implementation of Generation of Power Ramp Arb take the duration as the whole 
                                    //       waveform (including both the up and down slope), instead of the duration of up slope.
                                    //       So need double the duration to in line with the algorithm.
                                    ret = vsg.GeneratePowerRampArb(CW_ARB, 0, 0, 2000e-6, 4e6);
                                    if (!ret)
                                    {
                                        throw new Exception("VXT GeneratePowerRampArb (CW_ARB) Failed!");
                                    }

                                    ret = vsg.GeneratePowerRampArb(POWER_RAMP_ARB, -30, 0, 200e-6, 4e6);
                                    if (!ret)
                                    {
                                        throw new Exception("VXT GeneratePowerRampArb (POWER_RAMP_ARB) Failed!");
                                    }
                                }

                                //upload CW two tone waveform for IM3 measurement
                                cw_two_tone_waveform = INIParam.testParams[i].waveformName;
                                ret = vsg.LoadARB(waveformPath, cw_two_tone_waveform);
                                if (!ret)
                                {
                                    throw new Exception("LoadARB (Two Tone) Failed!");
                                }

                                //upload CW_and_modulation waveform for IM3 measurement
                                cw_modulation_waveform = INIParam.testParams[i].waveformName2;
                                ret = vsg.LoadARB(waveformPath, cw_modulation_waveform);
                                if (!ret)
                                {
                                    throw new Exception("LoadARB (CW And Modulation) Failed!");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_LoadTestParameters() failed! {0}", ex.Message);
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// Function to select particular technology, generate corresponding AWG signals
        /// if needed (for dynamic operation or Vramp)
        /// </summary>
        /// <param name="waveform">the arb that is currently selected</param>
        /// <param name="tech">the technology selected</param>
        /// <param name="iAwgVrampLoadedNumber"></param>
        /// <param name="bVrampMaskLoaded"></param>
        /// <param name="VrampMask"></param>
        /// <param name="is_DynamicEVM">whether to generate dynamic signal(using AWG)</param>
        /// <param name="is_seq">whether to generate a duty-cycled signal</param>
        /// <param name="cwType">the <see cref="CW_WAVEFORM_TYPE"/> when CW is selected/> </param>
        /// <param name="leadTime">the leading time when generate duty-cycled signal</param>
        /// <param name="lagTime">the lag time when generate duty-cycled signal</param>
        /// <param name="dutyCycle">the duty cycle to be generated</param>
        /// <param name="dTriggerDelay">the trigger delay in seconds</param>
        /// <param name="dPAEnableVoltage">the PA Enable voltage used when generating dynamic signal</param>
        /// <param name="is_PolarPA">whether to generate Vramp signal</param>
        /// <param name="VrampVoltage">Vramp voltage</param>
        /// <param name="triggerLineNumber">the PXI trigger line</param>
        /// <param name="awgPreload">whether to pre-load AWG signals (only for Keysight 33622A)</param>
        /// <param name="awgPreloadWaveform">the pre-loaded AWG waveform</param>
        /// <param name="use_secondary_source_analyzer">whether use secondary source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the secondary source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_SelectTechnology(ref string waveform, string tech,
            ref int iAwgVrampLoadedNumber,
            ref bool bVrampMaskLoaded,
            ref double[] VrampMask,
            bool is_DynamicEVM = false,
            bool is_seq = false,
            CW_WAVEFORM_TYPE cwType = CW_WAVEFORM_TYPE.CW,
            double leadTime = 0.0,
            double lagTime = 0.0,
            double dutyCycle = 0.99,
            double dTriggerDelay = 1e-6,
            double dPAEnableVoltage = 0.0,
            bool is_PolarPA = false,
            double VrampVoltage = 1.2,
            int triggerLineNumber = 2,
            bool awgPreload = false,
            string awgPreloadWaveform = "",
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                bool found = false;
                bool ret = true;
                string waveForm = null;
                string vRampMask = null;
                foreach (TestParameter t in INIParam.testParams)
                {
                    if (t.technology.Equals(tech))
                    {
                        if (vsa != null)
                            vsa.INIInfo = t;
                        else
                        {
                            waveForm = t.waveformName;
                            vRampMask = t.vrampMask;
                        } 
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    throw new Exception("Do not find technology Waveform!");
                }

                // Reset IQ Delay to 0
                vsg.SetIQDelay(0);

                SUPPORTEDFORMAT tech_format = TestConditionSetup_GetFormat(tech);

                if (is_seq)
                {
                    vsg.SyncTrigType = Source_SynchronizationTriggerTypeEnum.SynchronizationTriggerTypePerSequence;

                    if (tech_format == SUPPORTEDFORMAT.CW)
                    {
                        if (cwType == CW_WAVEFORM_TYPE.CW)
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, CW_ARB, leadTime, dutyCycle, false);

                            waveform = CW_ARB;
                            Logging(LogLevel.INFO, "Generate CW ARB Sequence {0}", CW_ARB);
                            if (!ret)
                            {
                                throw new Exception("Generate CW Sequence failed");
                            }
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_POWER_RAMP)
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, POWER_RAMP_ARB, leadTime, dutyCycle, false);

                            waveform = POWER_RAMP_ARB;
                            Logging(LogLevel.INFO, "Generate RAMP ARB Sequence {0}", POWER_RAMP_ARB);
                            if (!ret)
                            {
                                throw new Exception("Generate CW_POWER_RAMP Sequence failed");
                            }
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_TWO_TONE)
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, cw_two_tone_waveform, leadTime, dutyCycle, false);

                            if (vsa != null)
                                waveform = vsa.INIInfo.waveformName;
                            else waveform = waveForm;
                            Logging(LogLevel.INFO, "Generate Two Tone Sequence {0}", waveform);
                            if (!ret)
                            {
                                throw new Exception("Generate CW_TWO_TONE Sequence failed");
                            }
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_AND_MODULATION)
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, cw_modulation_waveform, leadTime, dutyCycle, false);

                            waveform = vsa.INIInfo.waveformName2;
                            Logging(LogLevel.INFO, "Generate CW and Modulation Sequence {0}", vsa.INIInfo.waveformName2);
                            if (!ret)
                            {
                                throw new Exception("Generate CW_AND_MODULATION Sequence failed");
                            }
                        }

                    }
                    //else if (tech.Equals("WLANN20MHZ") || tech.Equals("WLANN40MHZ")
                    //    || tech.Equals("WLANAC40MHZ") || tech.Equals("WLANAC80MHZ")
                    //    || tech.Equals("WLANAC160MHZ"))
                    else if (tech_format == SUPPORTEDFORMAT.WLAN11N ||
                        tech_format == SUPPORTEDFORMAT.WLAN11AC ||
                        tech_format == SUPPORTEDFORMAT.WLAN11AX ||
                        tech_format == SUPPORTEDFORMAT.WLAN11A ||
                        tech_format == SUPPORTEDFORMAT.WLAN11B ||
                        tech_format == SUPPORTEDFORMAT.WLAN11G ||
                        tech_format == SUPPORTEDFORMAT.WLAN11J ||
                        tech_format == SUPPORTEDFORMAT.WLAN11P ||
                        tech_format == SUPPORTEDFORMAT.LTETDD||
                        tech_format == SUPPORTEDFORMAT.LTEFDD||
                        tech_format == SUPPORTEDFORMAT.LTEAFDD||
                        tech_format == SUPPORTEDFORMAT.LTEATDD)
                    {
                        if (vsa != null)
                            waveform = vsa.INIInfo.waveformName;
                        else waveform = waveForm;
                        if (is_DynamicEVM == true)
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, waveform, dTriggerDelay, dutyCycle, true);

                            //setup AWG for PAEnable signal
                            AwgGenDynamicPulse(awgPreload, awgPreloadWaveform, waveform, leadTime, lagTime,
                                dutyCycle, dPAEnableVoltage, triggerLineNumber, dTriggerDelay,
                                use_secondary_source_analyzer, secondary_source_analyzer_config);
                        }
                        else
                        {
                            ret = genPulseSequenceAndPlay(vsg, vsa, waveform, leadTime, dutyCycle, false);
                        }
                        
                        if (!ret)
                        {
                            throw new Exception("Generate WLAN Waveform Sequence failed!");
                        }
                    }
                    else
                    {
                        if (vsa != null)
                            waveform = vsa.INIInfo.waveformName;
                        else waveform = waveForm;
                        ret = genPulseSequenceAndPlay(vsg, vsa, waveform, leadTime, dutyCycle, false);

                        Logging(LogLevel.INFO, "Play waveform {0} Sequence", waveform);

                        if (!ret)
                        {
                            throw new Exception("Generate Waveform" + waveform + " Sequence failed!");
                        }

                    }
                }

                else
                {
                    vsg.SyncTrigType = Source_SynchronizationTriggerTypeEnum.SynchronizationTriggerTypePerArb;
                    
                    if (tech_format == SUPPORTEDFORMAT.CW)
                    {
                        if (cwType == CW_WAVEFORM_TYPE.CW)
                        {
                            waveform = CW_ARB;
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_POWER_RAMP)
                        {
                            waveform = POWER_RAMP_ARB;
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_TWO_TONE)
                        {
                            waveform = cw_two_tone_waveform;
                        }
                        else if (cwType == CW_WAVEFORM_TYPE.CW_AND_MODULATION)
                        {
                            waveform = cw_modulation_waveform;
                        }
                    }
                    else
                    {
                        if (vsa != null)
                            waveform = vsa.INIInfo.waveformName;
                        else waveform = waveForm;
                    }

                    //vxt.GetARBCatalog();
                    //for (int i = 0; i < vxt.WaveformList.Count; i++)
                    //    Logging(LogLevel.INFO, "waveform[{0}]: {1}", i, vxt.WaveformList[i].ToString());

                    ret = vsg.PlayARB(waveform);

                    ArbInfo info = new ArbInfo();
                    // Get RMS value from playing waveform
                    ret = ret && vsg.GetArbInfo(waveform, ref info);
                    vsg.Arb = info;

                    // REVISIT-Jianhui-20170809:
                    // We found an issue that in M9391A, following IVI function call sequence result in failure.
                    // So do _not_ apply for the moment since the Source/Analyzer setup or measurement steps will
                    // do VSA.Apply() anyway. But we need to check this with M9391A/M9393A IVI team to understand
                    // the root cause of this issue.
                    if (vsa != null)
                    ret = ret && vsa.SetRms(vsg.Arb.arbRmsValue, false);

                    if (tech_format == SUPPORTEDFORMAT.GSM)
                    {
                        //waveform = vsa.INIInfo.waveformName;

                        if (is_PolarPA)
                        {
                            if (bVrampMaskLoaded == false)
                            {
                                // assume the sampleRate for data is 4 times symbol rate

                                string vRampMaskTemp = null;
                                if (vsa != null)
                                    vRampMaskTemp = vsa.INIInfo.vrampMask;
                                else
                                    vRampMaskTemp = vRampMask;
                                var rs = new FileStream(vRampMaskTemp, FileMode.Open);
                                TextReader rd = new StreamReader(rs);

                                // Read the data size values
                                string TimeAndAmpl = null;
                                int numberLine = 0;
                                while ((TimeAndAmpl = rd.ReadLine()) != null)
                                {
                                    numberLine++;
                                }
                                rd.Close();
                                rs.Close();

                                if (vsa != null)
                                    vRampMaskTemp = vsa.INIInfo.vrampMask;
                                else
                                    vRampMaskTemp = vRampMask;
                                var rs2 = new FileStream(vRampMaskTemp, FileMode.Open);
                                VrampMask = new double[numberLine];
                                rd = new StreamReader(rs2);

                                int i = 0;
                                string[] columns = null;
                                while ((TimeAndAmpl = rd.ReadLine()) != null)
                                {
                                    columns = TimeAndAmpl.Split(',');
                                    VrampMask[i] = Convert.ToDouble(columns[1]);
                                    i++;
                                }
                                rd.Close();
                                rs2.Close();

                                //awg.
                                awg.playVrampMask(triggerLineNumber, ref VrampMask, VrampVoltage);

                                //we found out, the first round can not trigger AWG, so I must execute above configuration two times
                                iAwgVrampLoadedNumber++;
                                //if (iAwgVrampLoadedNumber >= 2)
                                //    bVrampMaskLoaded = true;
                            }

                            //awg. only set channel amplitude
                            awg.SetChannelAmplitude(0, VrampVoltage);
                            awg.SetChannelAmplitude(1, VrampVoltage);
                        }
                    }

                    if (!ret)
                    {
                        throw new Exception("Generate Waveform " + waveform + " failed!");
                    }
                }

                return ret;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_SelectTechnology Failed! {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Function return the loaded technologies for particular direction 
        /// </summary>
        /// <param name="dir">the <see cref="DIRECTION"/> </param>
        /// <returns>true for success, false for fail</returns>
        public List<string> TestConditionSetup_GetLoadedTechnologies(DIRECTION dir = DIRECTION.UPLINK)
        {
            List<string> tech = new List<string>();
            string direction = (dir == DIRECTION.UPLINK ? "UPLINK" : "DOWNLINK");

            foreach (TestParameter t in INIParam.testParams)
            {
                if (t!= null && t.Direction.ToUpper().Equals(direction))
                    tech.Add(t.technology);
            }
            return tech;
        }

        /// <summary>
        /// Function to setup source and receiver settings
        /// </summary>
        /// <param name="src_freq">the source frequency to be set</param>
        /// <param name="src_pwr">the source output poewr</param>
        /// <param name="output_enabled">whether source's output is enabled</param>
        /// <param name="rcv_freq">the receiver frequency to be set</param>
        /// <param name="rcv_pwr">the receiver expected power to be set</param>
        /// <param name="rcv_acq_mode">the receiver acquisition mode</param>
        /// <param name="rcv_fft_acq_length">the receiver FFT acquistion length</param>
        /// <param name="rcv_fft_win_shape">the receiver FFT acquistion window shape</param>
        /// <param name="setup_src">whether to setup source's related setting</param>
        /// <param name="setup_rcv">whether to setup receiver's related setting</param>
        /// <param name="apply">whether to 'apply'</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_SourceReceiver(double src_freq, double src_pwr, bool output_enabled, double rcv_freq, double rcv_pwr, AnalyzerAcquisitionModeEnum rcv_acq_mode, AnalyzerFFTAcquisitionLengthEnum rcv_fft_acq_length,
            AnalyzerFFTWindowShapeEnum rcv_fft_win_shape, bool setup_src, bool setup_rcv, bool apply,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool ret1 = true;
            bool ret2 = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            if (vxt == null)
            {
                if (setup_src)
                {
                    ret1 = vsg.SetupSource(src_freq, src_pwr, output_enabled, PortEnum.PortRFOutput, apply);
                }
                if (setup_rcv)
                {
                    ret2 = vsa.SetupReceiver(rcv_acq_mode, rcv_freq, rcv_pwr,
                        rcv_fft_acq_length, rcv_fft_win_shape,
                        PortEnum.PortRFInput, apply);
                }
            }
            else
            {
                if (setup_src)
                {
                    ret1 = vxt.SetupSource(src_freq, src_pwr, output_enabled, PortEnum.PortRFOutput, apply);
                }
                if (setup_rcv)
                {
                    ret2 = vxt.SetupReceiver(rcv_acq_mode, rcv_freq, rcv_pwr, rcv_fft_acq_length, rcv_fft_win_shape, PortEnum.PortRFInput, apply);
                }
            }

            if (ret1 == false)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_SourceReceiver: Setup Source failed!");
            }
            if (ret2 == false)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_SourceReceiver: Setup Receiver failed!");
            }
            return ret1 && ret2;
        }

        /// <summary>
        /// Function to setup the trigger configuration for source/analyzer
        /// </summary>
        /// <param name="ext_trig_enabled">source external trigger enabled</param>
        /// <param name="sync_output_trig_enabled">source sync-output trigger enabled</param>
        /// <param name="extTrigSource">the source's external trigger source</param>
        /// <param name="syncOutputTrigDest">the source's sync-output trigger destination</param>
        /// <param name="sync_output_trig_pulse_width">the source's sync-output trigger pulse width</param>
        /// <param name="rcv_trig_mode">recerive trigger mode</param>
        /// <param name="rcv_trig_delay">receiver trigger delay</param>
        /// <param name="rcv_trig_timeout_mode">receiver trigger timeout mode</param>
        /// <param name="rcv_trig_timeout_value">receiver tirgger timeout value</param>
        /// <param name="rcv_trig_source">the receiver's trigger source</param>
        /// <param name="setup_source">whether to setup source's trigger setting</param>
        /// <param name="setup_analyzer">whether to setup receiver's trigger setting</param>
        /// <param name="apply">whether to apply</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Trigger(bool ext_trig_enabled, bool sync_output_trig_enabled, SourceTriggerEnum extTrigSource, SourceTriggerEnum syncOutputTrigDest, double sync_output_trig_pulse_width,
            AnalyzerTriggerModeEnum rcv_trig_mode, double rcv_trig_delay, AnalyzerTriggerTimeoutModeEnum rcv_trig_timeout_mode, int rcv_trig_timeout_value,
            AnalyzerTriggerEnum rcv_trig_source, bool setup_source, bool setup_analyzer, bool apply,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool ret1 = true;
            bool ret2 = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            if (vxt == null)
            {
                if (setup_source)
                {
                    ret1 = vsg.SetupTrigger(ext_trig_enabled, sync_output_trig_enabled, extTrigSource, syncOutputTrigDest, sync_output_trig_pulse_width, apply);
                }
                if (setup_analyzer)
                {
                    ret2 = vsa.SetupTrigger(rcv_trig_mode, rcv_trig_delay, rcv_trig_timeout_mode, rcv_trig_timeout_value, rcv_trig_source, apply);
                }
                //Console.WriteLine("VSG+VSA Setup Source Trigger {0}, Setup Receiver Trigger {1}.", ret1, ret2);
            }
            else
            {
                if (setup_source)
                {
                    ret1 = vxt.SetupSrcTrigger(ext_trig_enabled, sync_output_trig_enabled, extTrigSource, syncOutputTrigDest, sync_output_trig_pulse_width, apply: apply);
                }
                if (setup_analyzer)
                {
                    ret2 = vxt.SetupRcvTrigger(rcv_trig_mode, rcv_trig_delay, rcv_trig_timeout_mode, rcv_trig_timeout_value, rcv_trig_source, apply);
                }
                Logging(LogLevel.INFO, "VXT Setup Trigger {0} {1}", ret1, ret2);
            }
            if (ret1 == false)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_Trigger: Setup Source Trigger failed!");
            }
            if (ret2 == false)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_Trigger: Setup Receiver Trigger failed!");
            }
            return ret1 && ret2;
        }



        /// <summary>
        /// Function to setup VNA
        /// </summary>
        /// <param name="filename">the VNA configuration file</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_Vna(string filename)
        {
            bool ret = vna.readVnaSetupData(filename);
            if (!ret)
                Logging(LogLevel.ERROR, "TestConditionSetup_Vna failed!");

            return true;
        }

        /// <summary>
        /// Function to load pathloss calibration file
        /// </summary>
        /// <param name="filename">path loss calibration data file</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_LoadCableLossCalData(string filename = null)
        {
            const int maxPath = 20;
            bool ret = true;
            try
            {
                if (filename != null && File.Exists(filename))
                {
                    Utility.CalResFrequency = new double[maxPath][];
                    Utility.CalResInputLoss = new double[maxPath][];
                    Utility.CalResOutputLoss = new double[maxPath][];

                    Utility.CalLossBeforeDUT = new double[maxPath][];
                    Utility.CalLossAfterDUT = new double[maxPath][];

                    Utility.readCalData(filename, ref Utility.CalResFrequency,
                                ref Utility.CalResInputLoss, ref Utility.CalResOutputLoss, ref Utility.CalLossBeforeDUT, ref Utility.CalLossAfterDUT);
                }
                else
                {
                    Logging(LogLevel.ERROR, "TestConditionSetup_LoadCableLossCalData Failed!");
                    ret = false;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_LoadCableLossCalData Failed, {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to setup cable loss
        /// </summary>
        /// <param name="use_cal_file">true: use the pathloss calibration data file; false: use nominal input/output loss value</param>
        /// <param name="filename">path loss calibration data file</param>
        /// <param name="nominal_input_loss">nominal DUT input cable loss</param>
        /// <param name="nominal_output_loss">nominal DUT output cable loss</param>
        /// <returns>true for success, false for fail</returns>
        public bool TestConditionSetup_CableLoss(bool use_cal_file, string filename = null, double nominal_input_loss=0, double nominal_output_loss=0)
        {
            const int maxPath = 20;
            bool ret = true;
            try
            {
                Utility.use_cal_file = use_cal_file;
                if (use_cal_file && filename != null && File.Exists(filename))
                {
                    Utility.CalResFrequency = new double[maxPath][];
                    Utility.CalResInputLoss = new double[maxPath][];
                    Utility.CalResOutputLoss = new double[maxPath][];

                    Utility.CalLossBeforeDUT = new double[maxPath][];
                    Utility.CalLossAfterDUT = new double[maxPath][];

                    Utility.readCalData(filename, ref Utility.CalResFrequency,
                                ref Utility.CalResInputLoss, ref Utility.CalResOutputLoss, ref Utility.CalLossBeforeDUT, ref Utility.CalLossAfterDUT);

                }
                else
                {
                    Utility.nominal_input_loss = nominal_input_loss;
                    Utility.nominal_output_loss = nominal_output_loss;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "TestConditionSetup_LoadCableLossCalData Failed, {0}", ex.Message);
                ret = false;
            }

            return ret;
        }
        #endregion

        #region Measurements

        /// <summary>
        /// Measure POut value with a given PIn.
        /// </summary>
        /// <param name="freq">measurement frequency</param>
        /// <param name="dutInputPower">the DUT input power</param>
        /// <param name="targetGain">the target DUT gain</param>
        /// <param name="tps">the test point <see cref="Dictionary{TestItem_Enum, TestPoint}"/> definition </param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureDutOutputPower(double freq, double dutInputPower, double targetGain, Dictionary<TestItem_Enum, TestPoint> tps,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double dutOutputPower = 0;
            bool passed = true;
            double dutInputLoss = 0;
            double dutOutputLoss = 0;
            Stopwatch sw = new Stopwatch();

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                GetCableLoss(ref dutInputLoss, ref dutOutputLoss, freq, freq);

                sw.Reset();
                sw.Start();
                vsg.Frequency = freq;
                vsg.BasebandFrequency = 0;
                vsa.Frequency = freq;
                vsa.PowerAcqOffsetFreq = 0;

                vsg.Amplitude = dutInputPower + dutInputLoss;
                vsg.BasebandPower = 0;
                vsa.Power = dutInputPower + targetGain - dutOutputLoss + vsa.INIInfo.ReferenceLevelMargin + vsg.Arb.arbRmsValue;

                if (vxt == null)
                {
                    vsg.Apply();
                    vsa.Apply();
                }
                else
                {
                    vxt.Apply();
                }

                bool overload = false;
                // Try to measure the Power of Dut Output Port
                // Since we are not sure if FFT data is available, do not use FFT mode
                vsa.MeasurePOut(dutOutputLoss, ref overload, ref dutOutputPower, false);
                sw.Stop();

                Logging(LogLevel.INFO, "With DUT Input Power at {0}, Measured DUT Output Power: {1},  Overload: {2}", dutInputPower, dutOutputPower, overload);

                passed = setAndCheckResult(tps, TestItem_Enum.POUT, dutOutputPower, sw.ElapsedMilliseconds, Logging) && passed;
                return passed;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureDutOutputPower Error! {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Power Servo (obsolete)
        /// </summary>
        /// <param name="freq"></param>
        /// <param name="targetPout"></param>
        /// <param name="targetGain"></param>
        /// <param name="poutMargin"></param>
        /// <param name="fftServo"></param>
        /// <param name="loopCount"></param>
        /// <param name="test_acpr"></param>
        /// <param name="test_harm"></param>
        /// <param name="alignVsg"></param>
        /// <returns></returns>
        //public PowerServoResult ServoInputPower(double freq, double targetPout, double targetGain, double poutMargin, bool fftServo, int loopCount, 
        //                                        bool test_acpr = false, bool test_harm = false, bool alignVsg = false)
        //{
        //    PowerServoResult powerServoResult = new PowerServoResult();

        //    bool overload = false;
        //    double measPout = 0;
        //    int servoCount = 0;
        //    double inputPower = 0;
        //    Stopwatch sw = new Stopwatch();

        //    // REVISIT: should get this from VSG
        //    double rfOffset = 0, scaleOffset = 1;

        //    powerServoResult.dFrequency = freq;
        //    powerServoResult.dTargetPower = targetPout;
        //    powerServoResult.bTestACPR = test_acpr;
        //    powerServoResult.bTestHarmonics = test_harm;

        //    double refLevelMargin = vsa.INIInfo.ReferenceLevelMargin;


        //     powerServoResult.dDUTInputLoss = Utility.getInputAtten(freq + vsg.BasebandFrequency);
        //     powerServoResult.dDUTOutputLoss = Utility.getOutputAtten(freq + vsa.PowerAcqOffsetFreq);

        //    if (vxt == null)
        //    {
        //        // Servo Loop
        //        sw.Reset();

        //        vsg.Frequency = freq;
        //        vsa.Frequency = freq;
        //        vsg.Amplitude = targetPout + powerServoResult.dDUTInputLoss - targetGain + servoHeadRoom;
        //        vsg.BasebandPower = -servoHeadRoom;
        //        vsa.Power = targetPout - powerServoResult.dDUTOutputLoss + refLevelMargin + vsg.Arb.arbRmsValue;

        //        vsg.Apply();
        //        vsa.Apply();

        //        sw.Start();
        //        do
        //        {
        //            vsa.MeasurePOut(powerServoResult.dDUTOutputLoss, ref overload, ref measPout, fftServo);

        //            Logging(LogLevel.INFO, "MeasurePOut return {0}, overload={1}", measPout, overload);
        //            double basebandPower = -10;
        //            if (Math.Abs(measPout - targetPout) > poutMargin)
        //            {
        //                //double basebandPower = -10;
        //                basebandPower = vsg.BasebandPower + targetPout - measPout;
        //                vsg.BasebandPower = Math.Min(basebandPower, 0.0);
        //                //if (alignVsg == true)
        //                //{
        //                //    vsg.UsePowerSearchResult(rfOffset, scaleOffset);
        //                //}
        //                vsg.Apply();
        //            }
        //            else
        //            {
        //                basebandPower = vsg.BasebandPower + targetPout - measPout;
        //                vsg.BasebandPower = Math.Min(basebandPower, 0.0);
        //            }
        //            servoCount++;
        //        } while (Math.Abs(measPout - targetPout) > poutMargin && vsg.BasebandPower < 0 && servoCount < loopCount);
        //        sw.Stop();
        //        powerServoResult.dTimeMeasured = sw.ElapsedMilliseconds;

        //        powerServoResult.nServoCount = servoCount;

        //        // Set Servo count to -1 if loop fails to converge
        //        if (vsg.BasebandPower == 0)
        //        {
        //            Logging(LogLevel.ERROR, "Power Servo failed, Baseband Power reach 0!");
        //            powerServoResult.bServoPass = false;
        //        }
        //        else if (servoCount == loopCount)
        //        {
        //            Logging(LogLevel.ERROR, "Power Servo failed, Maximum Servo Count {0} reached!", servoCount);
        //            powerServoResult.bServoPass = false;
        //        }
        //        else
        //        {
        //            powerServoResult.bServoPass = true;
        //            powerServoResult.dMeasuredPowerAtReceiver = measPout;
        //        }

        //        inputPower = vsg.Amplitude + vsg.BasebandPower - powerServoResult.dDUTInputLoss;

        //        Logging(LogLevel.INFO, "PowerServo Result={4} measPout={0}, overload={1}, inputPower={2}, gain={3}",
        //            measPout, overload, inputPower, measPout - inputPower, powerServoResult.bServoPass);
        //        powerServoResult.dDUTGain = measPout - inputPower;

        //        if (test_acpr)
        //        {
        //            double[] AcprArray = new double[6];
        //            if (vsg.SelectedWaveform.Contains("LTE"))
        //                vsa.MeasureLteAcpr(powerServoResult.dDUTOutputLoss, measPout, ref AcprArray, fftServo);
        //            else if (vsg.SelectedWaveform.Contains("GSM"))
        //                vsa.MeasureGsmAcpr(4, fftServo);
        //            else
        //                vsa.MeasureStdAcpr(powerServoResult.dDUTOutputLoss, measPout, ref AcprArray, fftServo);
        //            powerServoResult.dACPRResult = AcprArray;
        //        }

        //        //bingwan, didn't support Harmonics running simutaneously with powerServo.
        //        //if (test_harm)
        //        //{ }
        //    }
        //    else
        //    {
        //        // pIn:  VXT expected input power at receiver
        //        // pOut: VXT generated output at source
        //        double pIn = targetPout - powerServoResult.dDUTOutputLoss;
        //        double pOut = targetPout - targetGain + powerServoResult.dDUTInputLoss;


        //        vxt.Amplitude = targetPout - targetGain + powerServoResult.dDUTInputLoss;
        //        vxt.Power = targetPout - powerServoResult.dDUTOutputLoss + refLevelMargin + vxt.Arb.arbRmsValue;
        //        vxt.Apply();

        //        if (powerServoResult.dACPRResult == null)
        //            powerServoResult.dACPRResult = new double[6];

        //        Logging(LogLevel.INFO, "Before PowerServo, Src={0}, Rcv={1}, pIn={2}, pOut={3}, Dut Input Cable Loss={4}, Dut Output Cable Loss={5}, Margin={6} ",
        //            vxt.Amplitude, vxt.Power, pIn, pOut, powerServoResult.dDUTInputLoss, powerServoResult.dDUTOutputLoss, poutMargin);

        //        vxt.ServoInputPower(pIn, pOut, poutMargin, test_acpr, test_harm,
        //            ref powerServoResult.bServoPass, ref powerServoResult.bOverloaded, 
        //            ref powerServoResult.dMeasuredPowerAtReceiver, ref powerServoResult.nServoCount,
        //            ref powerServoResult.dACPRResult, ref powerServoResult.dTimeMeasured);

        //        powerServoResult.dDUTGain = (powerServoResult.dMeasuredPowerAtReceiver + powerServoResult.dDUTOutputLoss) -  (vxt.Amplitude - powerServoResult.dDUTInputLoss);
        //        Logging(LogLevel.INFO, "PowerServo Result={0} measPout={1}, overload={2}, servoCount={3}, TimeMeasured={4}, ServoPass={5}, DutGain={6}",
        //                            powerServoResult.bServoPass, powerServoResult.dMeasuredPowerAtReceiver,
        //                            powerServoResult.bOverloaded, powerServoResult.nServoCount, 
        //                            powerServoResult.dTimeMeasured,
        //                            powerServoResult.bServoPass,
        //                            powerServoResult.dDUTGain);

        //    }
        //    return powerServoResult;
        //}

        /// <summary>
        /// Power Servo Measurement, with the capability to simultaneously measure following parameters as well
        ///   - POut, PIn, Gain, ACPR, PAE, ICC, IBAT, ITOTAL, Harmonics
        /// </summary>
        /// <param name="freq">measurement frequency</param>
        /// <param name="targetPout">the target DUT POut value</param>
        /// <param name="targetGain">the target DUT Gain</param>
        /// <param name="fftServo">whether do power-servo in FFT mode</param>
        /// <param name="loopCount">maximum loop count for power servo</param>
        /// <param name="duty_cycle">the duty-cycle for the signal</param>
        /// <param name="edge">whether this is for "Edge"</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool CombinedMeasurement(double freq, double targetPout, double targetGain, bool fftServo, int loopCount, double duty_cycle, bool edge, Dictionary<TestItem_Enum,TestPoint> tps,
            HARMONICS_RESULT_FORMAT harm_result_format = HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double dutInputLoss = 0;
            double dutOutputLoss = 0;
            bool overload = false;
            double measPout = 0;
            int servoCount = 0;
            double inputPower = 0;
            TestPoint tp;
            double ps_limit_high = 0;
            double ps_limit_low = 0;
            bool passed = true;
            double tolerance = 0;
            IDCSMU[] dcsmus = measCommon.dcsmus;

            if (tps.Count() == 0)
            {
                Logging(LogLevel.ERROR, "ERROR: No entry in TestResult list!");
                return false;
            }

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            Stopwatch sw = new Stopwatch();

            double refLevelMargin = vsa.INIInfo.ReferenceLevelMargin;

            GetCableLoss(ref dutInputLoss, ref dutOutputLoss, freq + vsg.BasebandFrequency, freq + vsa.PowerAcqOffsetFreq);

            #region POut, PIn, Gain, ACPR, Harmonics
            if (vxt == null)
            {
                // Servo Loop
                sw.Reset();

                if (tps.TryGetValue(TestItem_Enum.POUT, out tp))
                {
                    ps_limit_high = tp.LimitHigh;
                    ps_limit_low = tp.LimitLow;
                    tolerance = Math.Min(Math.Abs(ps_limit_high - targetPout), Math.Abs(targetPout - ps_limit_low));

                    vsg.Frequency = freq;
                    vsa.Frequency = freq;
                    vsg.Amplitude = targetPout + dutInputLoss - targetGain + servoHeadRoom;
                    vsg.BasebandPower = -servoHeadRoom;
                    vsa.Power = targetPout - dutOutputLoss + refLevelMargin + vsg.Arb.arbRmsValue;

                    vsg.Apply();
                    vsa.Apply();

                    Logging(LogLevel.INFO, "Before PowerServo, Src={0}, Rcv={1}, Dut Input Cable Loss={2}, Dut Output Cable Loss={3}, Margin={4} ",
                        vsg.Amplitude, vsa.Power, dutInputLoss, dutOutputLoss, tolerance);

                    sw.Start();
                    do
                    {
                        vsa.MeasurePOut(dutOutputLoss, ref overload, ref measPout, fftServo);

                        Logging(LogLevel.INFO, "MeasurePOut return {0}, overload={1}", measPout, overload);
                        double basebandPower = -10;
                        if (Math.Abs(measPout - targetPout) > tolerance)
                        {
                            //double basebandPower = -10;
                            basebandPower = vsg.BasebandPower + targetPout - measPout;
                            vsg.BasebandPower = Math.Min(basebandPower, 0.0);
                            vsg.Apply();
                        }
                        else
                        {
                            basebandPower = vsg.BasebandPower + targetPout - measPout;
                            vsg.BasebandPower = Math.Min(basebandPower, 0.0);
                        }
                        servoCount++;
                    } while (Math.Abs(measPout - targetPout) > tolerance && vsg.BasebandPower < 0 && servoCount <= loopCount);

                    sw.Stop();

                    passed = setAndCheckResult(tps, TestItem_Enum.POUT, measPout, sw.ElapsedMilliseconds, Logging) && passed;

                    // Set Servo count to -1 if loop fails to converge
                    if (vsg.BasebandPower == 0)
                    {
                        Logging(LogLevel.ERROR, "Power Servo failed, Baseband Power reach 0!");
                        passed = false;
                    }
                    else if (servoCount > loopCount)
                    {
                        Logging(LogLevel.ERROR, "Power Servo failed, Maximum Servo Count {0} reached!", servoCount);
                        passed = false;
                    }
                    else
                    {
                    }

                    inputPower = vsg.Amplitude + vsg.BasebandPower - dutInputLoss;
                    passed = setAndCheckResult(tps, TestItem_Enum.PIN, inputPower, 0, Logging) && passed;
                    passed = setAndCheckResult(tps, TestItem_Enum.PA_GAIN, measPout - inputPower, 0, Logging) && passed;

                    Logging(LogLevel.INFO, "PowerServo Result={4} measPout={0}, overload={1}, inputPower={2}, gain={3}",
                        measPout, overload, inputPower, measPout - inputPower, passed);
                }
                else
                {
                    // If POUT not in the TestItem list, just Measure the POut in VSA port
                    vsa.MeasurePOut(dutOutputLoss, ref overload, ref measPout, fftServo);
                    Logging(LogLevel.INFO, "POut not in PowerServo TestPoints. MeasurePOut={0}, overload={1}", measPout, overload);
                }

                #region ACPR
                SUPPORTEDFORMAT format = TestConditionSetup_GetFormat(vsa.INIInfo.technology);
                if (tps.ContainsKey(TestItem_Enum.ACPR_L1) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H1) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_L2) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H2) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_L3) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H3))
                {
                    double[] AcprArray = new double[6];
                    if (format == SUPPORTEDFORMAT.LTEFDD 
                        || format == SUPPORTEDFORMAT.LTETDD
                        || format == SUPPORTEDFORMAT.LTEAFDD
                        || format == SUPPORTEDFORMAT.LTEATDD
                        )
                    {
                        sw.Reset();
                        sw.Start();
                        vsa.MeasureLteAcpr(dutOutputLoss, measPout, ref AcprArray, fftServo);
                        sw.Stop();

                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L1, AcprArray[0], sw.ElapsedMilliseconds, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H1, AcprArray[1], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L2, AcprArray[2], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H2, AcprArray[3], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L3, AcprArray[4], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H3, AcprArray[5], 0, Logging) && passed;
                    }
                    else if (format == SUPPORTEDFORMAT.GSM)
                    {
                        sw.Reset();
                        sw.Start();
                        vsa.MeasureGsmAcpr(4, fftServo, ref AcprArray);
                        sw.Stop();

                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L1, AcprArray[0], sw.ElapsedMilliseconds, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H1, AcprArray[1], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L2, AcprArray[2], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H2, AcprArray[3], 0, Logging) && passed;
                    }
                    else
                    {
                        sw.Reset();
                        sw.Start();
                        vsa.MeasureStdAcpr(dutOutputLoss, measPout, ref AcprArray, fftServo);
                        sw.Stop();

                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L1, AcprArray[0], sw.ElapsedMilliseconds, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H1, AcprArray[1], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L2, AcprArray[2], 0, Logging) && passed;
                        passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H2, AcprArray[3], 0, Logging) && passed;
                    }
                }
                #endregion

                #region Harmonics
                if (tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp))
                {
                    bool[] harmsToMeasure = new bool[5] { true, false, false, false, false };
                    double[] harmonicsDataArray = new double[5];

                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp)) harmsToMeasure[1] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp)) harmsToMeasure[2] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp)) harmsToMeasure[3] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp)) harmsToMeasure[4] = true;

                    sw.Reset();
                    sw.Start();
                    if (edge == false)
                    {
                        // Since we found Harmonics on Spectrum mode has issues, change to use FFT mode as well
                        if (fftServo)
                            vsa.MeasureFFTHarms(harmsToMeasure, ref harmonicsDataArray, dutOutputLoss, harm_result_format);
                        else
                            vsa.MeasureSpecHarms(fftServo, harmsToMeasure, ref harmonicsDataArray, dutOutputLoss, harm_result_format);
                    }
                    else
                    {
                        vsa.MeasurePwrHarms(harmsToMeasure, ref harmonicsDataArray, dutOutputLoss, harm_result_format);
                    }
                    sw.Stop();

                    if (harmonicsDataArray.Length >= 2)
                        passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_2, harmonicsDataArray[1], sw.ElapsedMilliseconds, Logging) && passed;
                    if (harmonicsDataArray.Length >= 3)
                        passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_3, harmonicsDataArray[2], 0, Logging) && passed;
                    if (harmonicsDataArray.Length >= 4)
                        passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_4, harmonicsDataArray[3], 0, Logging) && passed;
                    if (harmonicsDataArray.Length >= 5)
                        passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_5, harmonicsDataArray[4], 0, Logging) && passed;

                }
                #endregion
            }
            else
            {
                // pIn:  VXT expected input power at receiver
                // pOut: VXT generated output at source
                double pIn = targetPout - dutOutputLoss;
                double pOut = targetPout - targetGain + dutInputLoss;
                double[] AcprResult = new double[6];
                bool test_ps = true;
                bool test_acpr = false;
                bool test_harm = false;
                double poutMargin = 0;
                bool servoPass = false;
                bool overloaded = false;
                double measuredPowerAtReceiver = 0;
                double timeMeasured = 0;
                double dutGain = 0;

                if (!tps.TryGetValue(TestItem_Enum.POUT, out tp))
                {
                    test_ps = false;
                    vxt.MeasurePOut(dutOutputLoss, ref overload, ref measPout, fftServo);
                    Logging(LogLevel.INFO, "POut not in PowerServo TestPoints. MeasurePOut={0}, overload={1}", measPout, overload);
                }

                else
                {
                    ps_limit_high = tp.LimitHigh;
                    ps_limit_low = tp.LimitLow;
                    poutMargin = Math.Min(Math.Abs(ps_limit_high - targetPout), Math.Abs(targetPout - ps_limit_low));

                    // NOTE-Jianhui: 
                    //   Reconfigure VXT's FFTAcquisition parameters to avoid other measurement change FFT parameters silently
                    vxt.SetupReceiver(AnalyzerAcquisitionModeEnum.AcquisitionModeFFT, freq, vxt.Power, AnalyzerFFTAcquisitionLengthEnum.FFTAcquisitionLength_512,
                        AnalyzerFFTWindowShapeEnum.FFTWindowShapeHann, PortEnum.PortRFInput, false);

                    vsg.Frequency = freq;
                    vsa.Frequency = freq;
                    vxt.Amplitude = targetPout - targetGain + dutInputLoss;
                    vxt.Power = targetPout - dutOutputLoss + refLevelMargin + vxt.Arb.arbRmsValue;

                    vxt.Apply();

                    Logging(LogLevel.INFO, "Before PowerServo, Src={0}, Rcv={1}, pIn={2}, pOut={3}, Dut Input Cable Loss={4}, Dut Output Cable Loss={5}, Margin={6} ",
                        vxt.Amplitude, vxt.Power, pIn, pOut, dutInputLoss, dutOutputLoss, ps_limit_high);
                }

                #region ACPR
                if (tps.ContainsKey(TestItem_Enum.ACPR_L1) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H1) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_L2) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H2) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_L3) ||
                        tps.ContainsKey(TestItem_Enum.ACPR_H3))
                    test_acpr = true;
                #endregion

                #region Harmonics
                AnalyzerAcquisitionModeEnum harmMode = AnalyzerAcquisitionModeEnum.AcquisitionModeFFT;
                bool[] harmsToMeasure = new bool[5] { true, false, false, false, false };
                double[] harmonicsDataArray = new double[5];

                if (tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp) ||
                    tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp))
                {
                    test_harm = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp)) harmsToMeasure[1] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp)) harmsToMeasure[2] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp)) harmsToMeasure[3] = true;
                    if (tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp)) harmsToMeasure[4] = true;

                    if (edge == false)
                    {
                        // Since we found Harmonics on Spectrum mode has issues, change to use FFT mode as well
                        if (fftServo)
                            harmMode = AnalyzerAcquisitionModeEnum.AcquisitionModeFFT;
                        else
                            harmMode = AnalyzerAcquisitionModeEnum.AcquisitionModeSpectrum;
                    }
                    else
                    {
                        harmMode = AnalyzerAcquisitionModeEnum.AcquisitionModePower;
                    }

                }
                #endregion

                vxt.ServoInputPower(pIn, pOut, poutMargin, test_ps, test_acpr, test_harm, harmMode, harmsToMeasure,
                    ref servoPass, ref overloaded,
                    ref measuredPowerAtReceiver, ref servoCount,
                    ref AcprResult, ref harmonicsDataArray, ref timeMeasured);

                // This is needed because we may use this result in PAE
                if (test_ps)
                {
                    measPout = measuredPowerAtReceiver + dutOutputLoss;
                }

                inputPower = vxt.Amplitude + vxt.BasebandPower - dutInputLoss;
                dutGain = measPout - inputPower;

                if (test_harm)
                {
                    // Add Dut Output Cable Loss
                    for (int i=0; i<harmonicsDataArray.Length; i++)
                    {
                        harmonicsDataArray[i] += dutOutputLoss;
                    }

                    #region Convert Harmonics Result to required format
                    double fundamentalPwr = harmonicsDataArray[0];
                    for (int i = 0; i < harmsToMeasure.Length; i++)
                    {
                        if (harmsToMeasure[i] && (i > 0))
                        {
                            switch (harm_result_format)
                            {
                                default:
                                case HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL:
                                    harmonicsDataArray[i] = harmonicsDataArray[i] - fundamentalPwr;
                                    break;

                                case HARMONICS_RESULT_FORMAT.DBM_PER_MHZ:
                                    harmonicsDataArray[i] = harmonicsDataArray[i] - 10 * Math.Log10(vsa.INIInfo.FilterBW * 1e-6);
                                    break;
                            }
                        }
                    }
                    #endregion

                }

                if (test_ps)
                {
                    Logging(LogLevel.INFO, "PowerServo Result={0} dutOutput={1}, overload={2}, servoCount={3}, TimeMeasured={4}, ServoPass={5}, DutGain={6}",
                                        servoPass, measPout,
                                        overloaded, servoCount,
                                        timeMeasured,
                                        servoPass,
                                        dutGain);
                }
                else
                {
                    Logging(LogLevel.INFO, "Skip PowerServo, dutOutput={0}", measPout);
                }

                passed = setAndCheckResult(tps, TestItem_Enum.POUT, measPout, timeMeasured, Logging) && passed;
                passed = setAndCheckResult(tps, TestItem_Enum.PIN, inputPower, 0, Logging) && passed;
                passed = setAndCheckResult(tps, TestItem_Enum.PA_GAIN, dutGain, 0, Logging) && passed;

                if (AcprResult.Length >= 1)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L1, AcprResult[0], 0, Logging) && passed;
                if (AcprResult.Length >= 2)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H1, AcprResult[1], 0, Logging) && passed;
                if (AcprResult.Length >= 3)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L2, AcprResult[2], 0, Logging) && passed;
                if (AcprResult.Length >= 4)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H2, AcprResult[3], 0, Logging) && passed;
                if (AcprResult.Length >= 5)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_L3, AcprResult[4], 0, Logging) && passed;
                if (AcprResult.Length >= 6)
                    passed = setAndCheckResult(tps, TestItem_Enum.ACPR_H3, AcprResult[5], 0, Logging) && passed;

                if (harmonicsDataArray.Length >= 2)
                    passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_2, harmonicsDataArray[1], 0, Logging) && passed;
                if (harmonicsDataArray.Length >= 3)
                    passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_3, harmonicsDataArray[2], 0, Logging) && passed;
                if (harmonicsDataArray.Length >= 4)
                    passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_4, harmonicsDataArray[3], 0, Logging) && passed;
                if (harmonicsDataArray.Length >= 5)
                    passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_5, harmonicsDataArray[4], 0, Logging) && passed;

            }
            #endregion

            #region PAE, ICC, IBAT, ITOTAL

            if (tps.TryGetValue(TestItem_Enum.PAE, out tp) ||
                    tps.TryGetValue(TestItem_Enum.ICC, out tp) ||
                    tps.TryGetValue(TestItem_Enum.IBAT, out tp) ||
                    tps.TryGetValue(TestItem_Enum.ITOTAL, out tp))
            {

                double rfDutyCycle = duty_cycle;
                double dcpower = 0.0;
                double current_icc = 0;
                double current_ibat = 0;
                double current_icustom1 = 0;
                double current_icustom2 = 0;
                double total_dcpower = 0.0;
                double total_current_icc = 0;
                double total_current_ibat = 0;
                double total_current_icustom1 = 0;
                double total_current_icustom2 = 0;
                double pae_result = 0;

                sw.Reset();
                sw.Start();
                for (int i = 0; i < dcsmus.Length; i++)
                {
                    if (dcsmus[i] != null)
                    {
                        if (tps.ContainsKey(TestItem_Enum.PAE))
                            dcsmus[i].MeasDCPower(ref dcpower);

                        if (tps.ContainsKey(TestItem_Enum.ICC) || tps.ContainsKey(TestItem_Enum.ITOTAL))
                            dcsmus[i].MeasCurrent(DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VCC, ref current_icc);

                        if (tps.ContainsKey(TestItem_Enum.IBAT) || tps.ContainsKey(TestItem_Enum.ITOTAL))
                            dcsmus[i].MeasCurrent(DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VBAT, ref current_ibat);

                        if (tps.ContainsKey(TestItem_Enum.ICUSTOM1) || tps.ContainsKey(TestItem_Enum.ITOTAL))
                            dcsmus[i].MeasCurrent(DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_1, ref current_icustom1);

                        if (tps.ContainsKey(TestItem_Enum.ICUSTOM2) || tps.ContainsKey(TestItem_Enum.ITOTAL))
                            dcsmus[i].MeasCurrent(DCSMUInfo.SMU_CHANTYPE.CHAN_CUSTOMER_2, ref current_icustom2);

                        total_dcpower += dcpower;
                        total_current_icc += current_icc;
                        total_current_ibat += current_ibat;
                        total_current_icustom1 += current_icustom1;
                        total_current_icustom2 += current_icustom2;
                        Logging(LogLevel.INFO, "SMU{0} - Power Consumption(VCC*ICC)(Watts): {1}, ICC: {2}, IBAT: {3}, ICUSTOM1: {4}, ICUSTOM2: {5}",
                            i + 1, dcpower, current_icc, current_ibat, current_icustom1, current_icustom2);
                    }
                }
                Logging(LogLevel.INFO, "Total Power Consumption (VCC*ICC) (Watts): {0}, Total ICC: {1}, Total IBAT: {2}, Total ICUSTOM1: {3}, Total ICUSTOM2: {4}",
                    total_dcpower, total_current_icc, total_current_ibat, total_current_icustom1, total_current_icustom2);

                double rfPowerWatts = Math.Pow(10, (measPout / 10)) * .001;
                Logging(LogLevel.INFO, "Total RF Power(Watts): {0}  POut: {1}dBm   DutyCycle:{2}", rfPowerWatts, measPout, rfDutyCycle);

                pae_result = (rfPowerWatts / total_dcpower) * 100 * rfDutyCycle;
                Logging(LogLevel.INFO, "PAE result: {0} %", pae_result);
                sw.Stop();

                if (tps.ContainsKey(TestItem_Enum.PAE))
                    passed = setAndCheckResult(tps, TestItem_Enum.PAE, pae_result, sw.ElapsedMilliseconds, Logging) && passed;
                if (tps.ContainsKey(TestItem_Enum.ICC))
                    passed = setAndCheckResult(tps, TestItem_Enum.ICC, total_current_icc, sw.ElapsedMilliseconds, Logging) && passed;
                if (tps.ContainsKey(TestItem_Enum.IBAT))
                    passed = setAndCheckResult(tps, TestItem_Enum.IBAT, total_current_ibat, sw.ElapsedMilliseconds, Logging) && passed;
                if (tps.ContainsKey(TestItem_Enum.ICUSTOM1))
                    passed = setAndCheckResult(tps, TestItem_Enum.ICUSTOM1, total_current_icustom1, sw.ElapsedMilliseconds, Logging) && passed;
                if (tps.ContainsKey(TestItem_Enum.ICUSTOM2))
                    passed = setAndCheckResult(tps, TestItem_Enum.ICUSTOM2, total_current_icustom2, sw.ElapsedMilliseconds, Logging) && passed;
                if (tps.ContainsKey(TestItem_Enum.ITOTAL))
                    passed = setAndCheckResult(tps, TestItem_Enum.ITOTAL, total_current_ibat + total_current_icc, sw.ElapsedMilliseconds, Logging) && passed;
            }

            #endregion

            return passed;
        }


        //public GainCompressionResult MeasureGainComp(bool use_cal_data = false, double dut_input_cable_loss = 0, double dut_output_cable_loss = 0)
        //{
        //    bool ret = true;
        //    GainCompressionResult result = new GainCompressionResult();
        //    double dDUTInputLoss = 0.0;
        //    double dDUTOutputLoss = 0.0;

        //    if (use_cal_data)
        //    {
        //        dDUTInputLoss = Utility.getInputAtten(vsg.Frequency + vsg.BasebandFrequency);
        //        dDUTOutputLoss = Utility.getOutputAtten(vsa.Frequency + vsa.PowerAcqOffsetFreq);
        //    }
        //    else
        //    {
        //        dDUTInputLoss = dut_input_cable_loss;
        //        dDUTOutputLoss = dut_output_cable_loss;
        //    }

        //    if (vxt == null)
        //    {
        //        ret = vsa.MeasureGainComp(100e-6, -30, vsg.Arb.arbSampleRate,
        //            vsg.Amplitude + vsg.BasebandPower - dDUTInputLoss, dDUTOutputLoss,
        //            ref result);
        //    }
        //    else
        //    {
        //        ret = vsa.MeasureGainComp(100e-6, -30, vsg.Arb.arbSampleRate,
        //            vsg.Amplitude + vsg.BasebandPower - dDUTInputLoss, dDUTOutputLoss,
        //            ref result);
        //    }

        //    return result;
        //}

        /// <summary>
        /// Measure Gain Compression Point, following can be measured at the same time
        ///   - 1dB Compression Point, 2dB Compression Point, 3dB Compression Point
        /// </summary>
        /// <param name="dut_input_power">DUT input power</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureGainComp(double dut_input_power, Dictionary<TestItem_Enum, TestPoint> tps,
            bool use_secondary_source_analyzer = false,
            SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool ret = true;
            GainCompressionResult result = new GainCompressionResult();
            double dDUTInputLoss = 0.0;
            double dDUTOutputLoss = 0.0;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                GetCableLoss(ref dDUTInputLoss, ref dDUTOutputLoss, vsg.Frequency + vsg.BasebandFrequency, vsa.Frequency + vsa.PowerAcqOffsetFreq);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                vsg.BasebandPower = 0.0;
                vsg.Amplitude = dut_input_power + dDUTInputLoss;
                vsg.Apply();

                ret = vsa.MeasureGainComp(100e-6, -30, vsg.Arb.arbSampleRate,
                    dut_input_power, dDUTOutputLoss,
                    ref result);

                sw.Stop();

                Logging(LogLevel.INFO, "Result={0}, linearGain = {1}, inPower1dB={2}, outPower1dB={3}, inPower2dB={4}, outPower2dB={5},inPower3dB={6}, outPower3dB={7},",
                    result.bGainCompressionPass,
                    result.linearGain,
                    result.inPower1dB,
                    result.gainComp1dB,
                    result.inPower2dB,
                    result.gainComp2dB,
                    result.inPower3dB,
                    result.gainComp3dB);

                ret = setAndCheckResult(tps, TestItem_Enum.GAIN_COMP_1DB, result.gainComp1dB, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.GAIN_COMP_2DB, result.gainComp2dB, sw.ElapsedMilliseconds, Logging) && ret;
                ret = setAndCheckResult(tps, TestItem_Enum.GAIN_COMP_3DB, result.gainComp3dB, sw.ElapsedMilliseconds, Logging) && ret;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureGainComp() failed! {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        //        /// <summary>
        //        /// Measure ACPR (obsolete)
        //        /// </summary>
        //        /// <param name="format"></param>
        //        /// <param name="do_powerservo"></param>
        //        /// <param name="acpr_result"></param>
        //        /// <param name="passed"></param>
        //        /// <param name="acpr_limit"></param>
        //        /// <param name="time_consumption"></param>
        //        /// <param name="gsm_num_avg"></param>
        //        /// <param name="freq"></param>
        //        /// <param name="targetPout"></param>
        //        /// <param name="targetGain"></param>
        //        /// <param name="poutMargin"></param>
        //        /// <param name="fftServo"></param>
        //        /// <param name="loopCount"></param>
        //        /// <param name="use_cal_data"></param>
        //        /// <param name="dut_input_cable_loss"></param>
        //        /// <param name="dut_output_cable_loss"></param>
        //        public void MeasureAcpr(ACPR_FORMAT_TYPE format, bool do_powerservo, ref double[] acpr_result, ref bool passed, double []acpr_limit, 
        //            ref double time_consumption, int gsm_num_avg = 1,
        //            double freq = 1700e6, double targetPout = -20, double targetGain = 10, double poutMargin = 0.5, bool fftServo = true, int loopCount = 10,
        //            bool use_cal_data = false, double dut_input_cable_loss = 0, double dut_output_cable_loss = 0)
        //        {
        //            double pOut = 0;
        //            bool overload = false;
        //            double outputLoss = 0;
        //            double inputLoss = 0;
        //            Stopwatch sw = new Stopwatch();
        //            sw.Reset();
        //            passed = true;

        //////            if (use_cal_data)
        ////            {
        //                inputLoss = Utility.getInputAtten(freq + vsg.BasebandFrequency);
        //                outputLoss = Utility.getOutputAtten(freq + vsa.PowerAcqOffsetFreq);
        //            //}
        //            //else
        //            //{
        //            //    inputLoss = dut_input_cable_loss;
        //            //    outputLoss = dut_output_cable_loss;
        //            //}

        //            sw.Start();

        //            if (do_powerservo || vxt != null)
        //            {
        //                PowerServoResult rs = ServoInputPower(freq, targetPout, targetGain, poutMargin, fftServo, loopCount,
        //                                                            true, false, false);
        //                acpr_result = rs.dACPRResult;
        //                if (!rs.bServoPass)  passed = false;
        //            }

        //            if (vxt == null)
        //            {
        //                vsa.MeasurePOut(outputLoss, ref overload, ref pOut, fftServo);

        //                switch (format)
        //                {

        //                    case ACPR_FORMAT_TYPE.ACPR_STANDARD_FORMAT:
        //                        vsa.MeasureStdAcpr(outputLoss, pOut, ref acpr_result, fftServo);
        //                        break;

        //                    case ACPR_FORMAT_TYPE.ACPR_LTE_FORMAT:
        //                        vsa.MeasureLteAcpr(outputLoss, pOut, ref acpr_result, fftServo);
        //                        break;

        //                    case ACPR_FORMAT_TYPE.ACPR_GSM_FORMAT:
        //                        vsa.MeasureGsmAcpr(gsm_num_avg, fftServo);
        //                        break;
        //                }
        //            }
        //            sw.Stop();
        //            time_consumption = sw.ElapsedMilliseconds;

        //            for (int i = 0; i < acpr_result.Length; i++)
        //            {
        //                if ((i < acpr_limit.Length) && (acpr_result[i] > acpr_limit[i]))
        //                {
        //                    passed = false;
        //                    return;
        //                }
        //            }
        //            return;
        //        }

        /// <summary>
        /// Measure ACPR(4 or 6 adjacent channels), doing PowerServo if required. Following 
        /// parameter can be measured at the same time.
        ///  - ACPR L1, ACPR H1
        ///  - ACPR L2, ACPR H2
        ///  - ACPR L3, ACPR H3
        /// </summary>
        /// <param name="freq">test frequency</param>
        /// <param name="targetPout">the target DUT POut value</param>
        /// <param name="targetGain">the target DUT Gain</param>
        /// <param name="fftServo">whether do power-servo in FFT mode</param>
        /// <param name="loopCount">maximum loop count for power servo</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool ServoInputPower(double freq, double targetPout, double targetGain, bool fftServo, int loopCount,
                                Dictionary<TestItem_Enum, TestPoint> tps,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool passed = true;
            double outputLoss = 0;
            double inputLoss = 0;
            TestPoint tp;
            bool edge = false;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            if (vsa != null)
            {
                if (vsa.INIInfo.Format.Equals("GSM") ||
                    vsa.INIInfo.Format.Equals("EDGE"))
                {
                    edge = true;
                }
            }

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();

                passed = GetCableLoss(ref inputLoss, ref outputLoss, freq + vsg.BasebandFrequency, freq + vsa.PowerAcqOffsetFreq);

                sw.Start();

                if (!tps.TryGetValue(TestItem_Enum.POUT, out tp))
                {
                    Logging(LogLevel.ERROR, "POut not in result list!");
                    passed = false;
                }

                passed = passed && CombinedMeasurement(freq, targetPout, targetGain, fftServo, loopCount, 1, edge, tps, HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL, use_secondary_source_analyzer, secondary_source_analyzer_config);

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "ServoInputPower Failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        /// <summary>
        /// Measure ACPR(4 or 6 adjacent channels), doing PowerServo if required. Following 
        /// parameter can be measured at the same time.
        ///  - ACPR L1, ACPR H1
        ///  - ACPR L2, ACPR H2
        ///  - ACPR L3, ACPR H3
        /// </summary>
        /// <param name="format">the <see cref="ACPR_FORMAT_TYPE"/> for the signal </param>
        /// <param name="do_powerservo">whether to do power-servo before ACPR</param>
        /// <param name="gsm_num_avg">gsm number of average</param>
        /// <param name="freq">measurement frequency</param>
        /// <param name="targetPout">the target DUT POut value</param>
        /// <param name="targetGain">the target DUT Gain</param>
        /// <param name="fftServo">whether do power-servo in FFT mode</param>
        /// <param name="loopCount">maximum loop count for power servo</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureAcpr(ACPR_FORMAT_TYPE format, bool do_powerservo, int gsm_num_avg,
                                double freq, double targetPout, double targetGain, bool fftServo, int loopCount,
                                Dictionary<TestItem_Enum, TestPoint> tps,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double pOut = 0;
            double[] AcprArray = new double[6];
            bool passed = true;
            double outputLoss = 0;
            double inputLoss = 0;
            TestPoint tp;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();

                passed = GetCableLoss(ref inputLoss, ref outputLoss, freq + vsg.BasebandFrequency, freq + vsa.PowerAcqOffsetFreq);

                sw.Start();

                if (do_powerservo)
                {
                    if (!tps.TryGetValue(TestItem_Enum.POUT, out tp))
                    {
                        Logging(LogLevel.ERROR, "Require PowerServo but POut not in result list!");
                        passed = false;
                    }
                }

                if (!tps.TryGetValue(TestItem_Enum.ACPR_H1, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.ACPR_L1, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.ACPR_H2, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.ACPR_L1, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.ACPR_H3, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.ACPR_L3, out tp))
                {
                    Logging(LogLevel.ERROR, "ACPR not in result list!");
                    passed = false;
                }

                passed = passed && CombinedMeasurement(freq, targetPout, targetGain, fftServo, loopCount, 1,
                    format == ACPR_FORMAT_TYPE.ACPR_GSM_FORMAT ? true : false, tps, HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL,
                    use_secondary_source_analyzer, secondary_source_analyzer_config);

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureAcpr2 failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        //public void MeasureHarmonics(bool do_powerservo, ref bool passed, ref double time_consumption, bool[] harmsToMeasure, ref double [] harmonicsDataArray, double [] harmonicsLimit, 
        //    bool bEdge, double freq = 1700e6, double targetPout = -20, double targetGain = 10, double poutMargin = 0.5, bool fftServo = true, int loopCount = 10)
        //{
        //    double pOut = 0;
        //    bool overload = false;
        //    double outputLoss = 0;
        //    double inputLoss = 0;
        //    Stopwatch sw = new Stopwatch();
        //    sw.Reset();
        //    passed = true;

        //    inputLoss = Utility.getInputAtten(freq + vsg.BasebandFrequency);
        //    outputLoss = Utility.getOutputAtten(freq + vsa.PowerAcqOffsetFreq);

        //    sw.Start();

        //    if (do_powerservo)
        //    {
        //        PowerServoResult rs = ServoInputPower(freq, targetPout, targetGain, poutMargin, fftServo, loopCount,
        //                                              false, false, false);
        //        //acpr_result = rs.dACPRResult;
        //        if (!rs.bServoPass)
        //        {
        //            passed = false;
        //            return;
        //        }
        //        pOut = rs.dMeasuredPowerAtReceiver + outputLoss;
        //        Logging(LogLevel.INFO, "Power Servo measured DUT Output {0} dBm", pOut);
        //    }

        //    if (bEdge == false)
        //    {
        //        vsa.MeasureSpecHarms(fftServo, harmsToMeasure, ref harmonicsDataArray, outputLoss);
        //    }
        //    else
        //    {
        //        vsa.MeasurePwrHarms(harmsToMeasure, ref harmonicsDataArray, outputLoss);
        //    }

        //    for (int i=0; i<harmonicsDataArray.Length; i++)
        //        Logging(LogLevel.INFO, "Measure Harmonics: {0}th Order {1}", i, harmonicsDataArray[i]);

        //    for (int i = 0; i < harmsToMeasure.Length; i++)
        //        if (harmonicsDataArray[i] - harmonicsDataArray[0] > harmonicsLimit[i])
        //        {
        //            passed = false;
        //            return;
        //        }

        //    sw.Stop();
        //    time_consumption = sw.ElapsedMilliseconds;

        //    return;
        //}

        /// <summary>
        /// Measure Harmonics
        /// </summary>
        /// <param name="do_powerservo">whether to do power-servo</param>
        /// <param name="bEdge">whether this is an Edge signal</param>
        /// <param name="freq">measurement frequency</param>
        /// <param name="targetPout">the target DUT POut value</param>
        /// <param name="targetGain">the target DUT Gain</param>
        /// <param name="fftServo">whether do power-servo in FFT mode</param>
        /// <param name="loopCount">maximum loop count for power servo</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="result_format">the test result format <see cref="HARMONICS_RESULT_FORMAT"/> </param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureHarmonics(bool do_powerservo, bool bEdge, 
                                      double freq, double targetPout, double targetGain, bool fftServo, int loopCount,
                                      Dictionary<TestItem_Enum, TestPoint> tps,
                                      HARMONICS_RESULT_FORMAT result_format = HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL,
                                    bool use_secondary_source_analyzer = false,
                                    SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool passed = true;
            double outputLoss = 0;
            double inputLoss = 0;
            bool[] harmsToMeasure = new bool[5] { true, false, false, false, false };
            double[] harmonicsDataArray = new double[5];
            TestPoint tp;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();

                passed = GetCableLoss(ref inputLoss, ref outputLoss, freq + vsg.BasebandFrequency, freq + vsa.PowerAcqOffsetFreq);

                sw.Start();

                if (do_powerservo)
                {
                    if (!tps.TryGetValue(TestItem_Enum.POUT, out tp))
                    {
                        Logging(LogLevel.ERROR, "Require PowerServo but POut not in result list!");
                        passed = false;
                    }
                }

                if (!tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp))
                {
                    Logging(LogLevel.ERROR, "Harmonics not in result list!");
                    passed = false;
                }

                passed = passed && CombinedMeasurement(freq, targetPout, targetGain, fftServo, loopCount, 1, bEdge, tps, result_format,
                    use_secondary_source_analyzer, secondary_source_analyzer_config);

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureHarmonics failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        /// <summary>
        /// Function to measure Harmonics on CXA-m
        /// </summary>
        /// <param name="freq">the measurement frequency</param>
        /// <param name="refLevel">the reference level used</param>
        /// <param name="trig_mode">trigger mode used</param>
        /// <param name="trig_line">the PXI trigger line used</param>
        /// <param name="harmsToMeasure">an arry of bool to indicate which harmonic is measured</param>
        /// <param name="tps">test points definitions</param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureHarmonics_CXAm(double freq, double refLevel, AnalyzerTriggerModeEnum trig_mode, AnalyzerTriggerEnum trig_line,
            bool[] harmsToMeasure, Dictionary<TestItem_Enum, TestPoint> tps)
        {
            bool passed = true;
            double outputLoss = 0;
            double inputLoss = 0;
            double[] harmonicsDataArray = new double[5];
            TestPoint tp;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();

                passed = GetCableLoss(ref inputLoss, ref outputLoss, freq, freq);

                sw.Start();

                if (!tps.TryGetValue(TestItem_Enum.HARMONICS_2, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_3, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_4, out tp) &&
                    !tps.TryGetValue(TestItem_Enum.HARMONICS_5, out tp))
                {
                    Logging(LogLevel.ERROR, "Harmonics not in result list!");
                    passed = false;
                }

                if (cxa_m == null)
                    passed = false;
                else
                {
                    cxa_m.SetupReceiver(AnalyzerAcquisitionModeEnum.AcquisitionModeSpectrum, freq, refLevel, 
                        AnalyzerFFTAcquisitionLengthEnum.FFTAcquisitionLength_64, AnalyzerFFTWindowShapeEnum.FFTWindowShapeFlatTop, PortEnum.PortRFInput);
                    cxa_m.SetupTrigger(trig_mode, 0, AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeTimeoutAbort, 100, trig_line);

                    passed = cxa_m.MeasureSpecHarms(false, harmsToMeasure, ref harmonicsDataArray,
                        outputLoss, HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL);
                }

                sw.Stop();

                passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_2, harmonicsDataArray[1], sw.ElapsedMilliseconds, Logging) && passed;
                passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_3, harmonicsDataArray[2], sw.ElapsedMilliseconds, Logging) && passed;
                passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_4, harmonicsDataArray[3], sw.ElapsedMilliseconds, Logging) && passed;
                passed = setAndCheckResult(tps, TestItem_Enum.HARMONICS_5, harmonicsDataArray[4], sw.ElapsedMilliseconds, Logging) && passed;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureHarmonics failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }


        //public void MeasurePAE(bool do_powerservo, ref double pae_result, ref bool passed, double pae_limit, double duty_cycle,
        //    ref double time_consumption, double freq = 1700e6, double targetPout = -20, double targetGain = 10, double poutMargin = 0.5, bool fftServo = true, int loopCount = 10,
        //    bool use_cal_data = false, double dut_input_cable_loss = 0, double dut_output_cable_loss = 0)
        //{
        //    double pOut = 0;
        //    bool overload = false;
        //    double outputLoss = 0;
        //    double inputLoss = 0;
        //    Stopwatch sw = new Stopwatch();
        //    sw.Reset();
        //    passed = true;

        //    if (use_cal_data)
        //    {
        //        inputLoss = Utility.getInputAtten(freq + vsg.BasebandFrequency);
        //        outputLoss = Utility.getOutputAtten(freq + vsa.PowerAcqOffsetFreq);
        //    }
        //    else
        //    {
        //        inputLoss = dut_input_cable_loss;
        //        outputLoss = dut_output_cable_loss;
        //    }

        //    sw.Start();
        //    if (do_powerservo)
        //    {
        //        PowerServoResult rs = ServoInputPower(freq, targetPout, targetGain, poutMargin, fftServo, loopCount,
        //                                              false, false, false);
        //        if (!rs.bServoPass) passed = false;
        //    }

        //    vsa.MeasurePOut(outputLoss, ref overload, ref pOut, fftServo);
        //    double rfDutyCycle = duty_cycle;
        //    double dcsmu_dcpower = 0.0;
        //    double total_dcsmu_dcpower = 0.0;

        //    for (int i = 0; i < dcsmus.Length; i++)
        //    {
        //        if(dcsmus[i]!=null)
        //        {
        //            dcsmus[i].MeasDCPower(DCSMUInfo.SMU_CHANTYPE.CHAN_FOR_VCC, ref dcsmu_dcpower);
        //            total_dcsmu_dcpower += dcsmu_dcpower;
        //            Logging(LogLevel.INFO, "SMU{0} Power Consumption(Watts): {1}", i + 1, dcsmu_dcpower);
        //        }
        //    }
        //    Logging(LogLevel.INFO, "Total Power Consumption(Watts): {0}", total_dcsmu_dcpower);

        //    double rfPowerWatts = Math.Pow(10, (pOut / 10)) * .001;
        //    Logging(LogLevel.INFO, "Total RF Power(Watts): {0}  pOut: {1}dBm   DutyCycle:{2}", rfPowerWatts, pOut, rfDutyCycle);

        //    pae_result = (rfPowerWatts / total_dcsmu_dcpower) * 100 * rfDutyCycle;
        //    Logging(LogLevel.INFO, "PAE result: {0} %,  PAE Limit:{1}", pae_result, pae_limit);

        //    sw.Stop();
        //    time_consumption = sw.ElapsedMilliseconds;

        //    if (pae_result<pae_limit)
        //    {
        //        passed = false;
        //    }

        //    return;
        //}

        /// <summary>
        /// Function to measure PAE
        /// </summary>
        /// <param name="do_powerservo">whether do power-servo before PAE measurement</param>
        /// <param name="duty_cycle">the duty-cycle used</param>
        /// <param name="freq">measurement frequency</param>
        /// <param name="targetPout">the target DUT POut value</param>
        /// <param name="targetGain">the target DUT Gain</param>
        /// <param name="fftServo">whether do power-servo in FFT mode</param>
        /// <param name="loopCount">maximum loop count for power servo</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasurePAE(bool do_powerservo, double duty_cycle,
                                double freq, double targetPout, double targetGain, bool fftServo, int loopCount,
                                Dictionary<TestItem_Enum, TestPoint> tps,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double outputLoss = 0;
            double inputLoss = 0;
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            bool passed = true;
            double rfDutyCycle = duty_cycle;
            bool bEdge = false;
            TestPoint tp;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {

                passed = GetCableLoss(ref inputLoss, ref outputLoss, freq + vsg.BasebandFrequency, freq + vsa.PowerAcqOffsetFreq);

                sw.Start();

                if (do_powerservo)
                {
                    if (!tps.TryGetValue(TestItem_Enum.POUT, out tp))
                    {
                        Logging(LogLevel.ERROR, "Require PowerServo but POut not in result list!");
                        passed = false;
                    }
                }

                if (!tps.TryGetValue(TestItem_Enum.PAE, out tp))
                {
                    Logging(LogLevel.ERROR, "PAE not in result list!");
                    passed = false;
                }

                SUPPORTEDFORMAT tech_format = TestConditionSetup_GetFormat(vsa.INIInfo.technology);
                if (tech_format == SUPPORTEDFORMAT.GSM || tech_format == SUPPORTEDFORMAT.EDGE) bEdge = true;

                passed = passed && CombinedMeasurement(freq, targetPout, targetGain, fftServo, loopCount, 1, bEdge, tps, HARMONICS_RESULT_FORMAT.DBC_PER_CHANNEL,
                    use_secondary_source_analyzer, secondary_source_analyzer_config);

                sw.Stop();
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasurePAE failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        /// <summary>
        /// Function to measure S-Parameter
        /// TODO - Should convert this to Dictionary<TestItem_Enum, TestPoint> ...
        /// </summary>
        /// <param name="vna_chanNum">VNA channel number</param>
        /// <param name="test_result">bool[] for test result</param>
        /// <param name="isVnaInvisible">whether VNA is visible or not</param>
        public void MeasureSParams(int vna_chanNum, ref bool[] test_result, bool isVnaInvisible)
        {
            try
            {
                bool isVnaVisible = !isVnaInvisible;
                vna.setVnaVisible(isVnaVisible);
                //vna.setVnaLocal(isVnaVisible);
                vna.setActiveChannelLocal(vna_chanNum);
                vna.measSparms(vna_chanNum, ref test_result);
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureSParams failed! {0}", ex.Message);
            }
        }

        //public void passVnaSetupData(SparaConfigure sparaConfig)
        //{
        //    vna.passVnaSetupData(sparaConfig);
        //}

        //public void MeasureIM3(ref double[] IM3DataArray, bool use_cal_data = false, double dut_input_cable_loss = 0, double dut_output_cable_loss = 0)
        //{
        //    double dDUTInputLoss = 0.0;
        //    double dDUTOutputLoss = 0.0;

        //    if (use_cal_data)
        //    {
        //        dDUTInputLoss = Utility.getInputAtten(vsg.Frequency + vsg.BasebandFrequency);
        //        dDUTOutputLoss = Utility.getOutputAtten(vsa.Frequency + vsa.PowerAcqOffsetFreq);
        //    }
        //    else
        //    {
        //        dDUTInputLoss = dut_input_cable_loss;
        //        dDUTOutputLoss = dut_output_cable_loss;
        //    }

        //    double previousVsgFreq = vsg.Frequency;
        //    //the PA_TwoTone.wfm baseband waveform has a +25e6 frequency offset, so need tune source frequency to have a -25e6 offset
        //    vsg.Frequency = previousVsgFreq - 25e6;
        //    vsg.Apply();

        //    if (vxt == null)
        //    {

        //        vsa.MeasureIM3(dDUTOutputLoss, ref IM3DataArray);
        //    }
        //    else
        //    {
        //        vsa.MeasureIM3(dDUTOutputLoss, ref IM3DataArray);
        //    }

        //    //Change back the frequency of source
        //    vsg.Frequency = previousVsgFreq;
        //    vsg.Apply();
        //}

        /// <summary>
        /// Function to perform IM3 measurement based on 2-tone CW Arb
        /// </summary>
        /// <param name="tps">test points definitions</param>
        /// <param name="dToneSpace">the space between 2 tones</param>
        /// <param name="dSpan">the span used when doing IM3 measurement</param>
        /// <param name="dRBW">the resolution bandwidth used for IM3 measurement</param>
        /// <param name="bIM3OptMode">whether is in optimal mode</param>
        /// <param name="rformat">IM3 result format</param>
        /// <param name="rawData">the measurement raw data</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureIM3(Dictionary<TestItem_Enum, TestPoint> tps, double dToneSpace, double dSpan, double dRBW, bool bIM3OptMode, IM3_RESULT_FORMAT rformat,
                               ref double[] rawData,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double dDUTInputLoss = 0.0;
            double dDUTOutputLoss = 0.0;
            double[] IM3DataArray = new double[6];
            bool passed = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                GetCableLoss(ref dDUTInputLoss, ref dDUTOutputLoss, vsg.Frequency + vsg.BasebandFrequency, vsa.Frequency + vsa.PowerAcqOffsetFreq);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                // RowData Format:
                //  index 0 - Left  Tone DUT Input Power
                //  index 1 - Right Tone DUT Input Power
                //  index 2 - Left  Tone DUT Output Power
                //  index 3 - Right Tone DUT Output Power
                //  index 4 - Left Tone 3rd Order Power
                //  index 5 - Right Tone 3rd Order Power

                rawData[0] = rawData[1] = vsg.Amplitude - dDUTInputLoss - 3;

                Logging(LogLevel.INFO, "The DUT Input Power at {0}/{1} MHz: {2}dBm", 
                    vsg.Frequency/1e6 - 0.5 * dToneSpace,
                    vsg.Frequency/1e6 + 0.5 * dToneSpace,
                    vsg.Amplitude - dDUTInputLoss - 3);

                passed = vsa.MeasureIM3(dDUTOutputLoss, dToneSpace, dSpan, dRBW, bIM3OptMode, ref IM3DataArray);

                sw.Stop();

                rawData[2] = IM3DataArray[1];
                rawData[3] = IM3DataArray[3];
                rawData[4] = IM3DataArray[0];
                rawData[5] = IM3DataArray[4];

                if (rformat == IM3_RESULT_FORMAT.IMD3)
                {
                    passed = setAndCheckResult(tps, TestItem_Enum.IMD3_LOW, IM3DataArray[2], sw.ElapsedMilliseconds, Logging) && passed;
                    passed = setAndCheckResult(tps, TestItem_Enum.IMD3_HIGH, IM3DataArray[5], sw.ElapsedMilliseconds, Logging) && passed;
                }
                else
                {
                    double d1 = vsg.Amplitude - dDUTInputLoss + IM3DataArray[2]/2;
                    double d2 = vsg.Amplitude - dDUTInputLoss + IM3DataArray[5]/2;
                    passed = setAndCheckResult(tps, TestItem_Enum.IIP3_LOW, d1, sw.ElapsedMilliseconds, Logging) && passed;
                    passed = setAndCheckResult(tps, TestItem_Enum.IIP3_HIGH, d2, sw.ElapsedMilliseconds, Logging) && passed;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureIM3_2 failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        /// <summary>
        /// Function to perform IM3 measurement based on CW + Modulation Arb
        /// </summary>
        /// <param name="tps">test point definitions</param>
        /// <param name="dBandWidth">bandwidth of the modulation signal</param>
        /// <param name="dToneSpace">tone space</param>
        /// <param name="dFilterBW">filter bandwidth</param>
        /// <param name="bIM3OptMode">whether in IM3 optimal mode</param>
        /// <param name="rformat">result format</param>
        /// <param name="rawData">the measurement row data</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureIM3OfModulation(Dictionary<TestItem_Enum, TestPoint> tps, double dBandWidth, double dToneSpace, double dFilterBW, bool bIM3OptMode,
                               IM3_RESULT_FORMAT rformat, ref double[] rawData,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {

            double dDUTInputLoss = 0.0;
            double dDUTOutputLoss = 0.0;
            double[] IM3DataArray = new double[6];
            bool passed = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                GetCableLoss(ref dDUTInputLoss, ref dDUTOutputLoss, vsg.Frequency + vsg.BasebandFrequency, vsa.Frequency + vsa.PowerAcqOffsetFreq);

                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                // RowData Format:
                //  index 0 - Left Tone DUT Input Power
                //  index 1 - Right Modulation DUT Input Power
                //  index 2 - Left Tone DUT Output Power
                //  index 3 - Right Modulation DUT Output Power
                //  index 4 - Left 3rd Order Power
                //  index 5 - Right 3rd Order Power

                //DUT Input Power need to be calculated.
                rawData[0] = rawData[1] = vsg.Amplitude - dDUTInputLoss - 3;
                Logging(LogLevel.INFO, "The DUT Input Power for tone at {0} MHz and modulation of center frequency at {1} MHz: {2}dBm",
                    vsg.Frequency / 1e6 - 0.5 * dToneSpace,
                    vsg.Frequency / 1e6 + 0.5 * dToneSpace,
                    vsg.Amplitude - dDUTInputLoss - 3);

                passed = vsa.MeasureIM3OfModulation(dDUTOutputLoss, dBandWidth, dToneSpace, dFilterBW, bIM3OptMode, ref IM3DataArray);
                sw.Stop();

                rawData[2] = IM3DataArray[1];
                rawData[3] = IM3DataArray[3];
                rawData[4] = IM3DataArray[0];
                rawData[5] = IM3DataArray[4];

                if (rformat == IM3_RESULT_FORMAT.IMD3)
                {
                    passed = setAndCheckResult(tps, TestItem_Enum.IMD3_LOW, IM3DataArray[2], sw.ElapsedMilliseconds, Logging) && passed;
                    passed = setAndCheckResult(tps, TestItem_Enum.IMD3_HIGH, IM3DataArray[5], sw.ElapsedMilliseconds, Logging) && passed;
                }
                else
                {
                    double d1 = vsg.Amplitude - dDUTInputLoss + IM3DataArray[2] / 2;
                    double d2 = vsg.Amplitude - dDUTInputLoss + IM3DataArray[5] / 2;
                    passed = setAndCheckResult(tps, TestItem_Enum.IIP3_LOW, d1, sw.ElapsedMilliseconds, Logging) && passed;
                    passed = setAndCheckResult(tps, TestItem_Enum.IIP3_HIGH, d2, sw.ElapsedMilliseconds, Logging) && passed;
                }

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureIM3_2 failed! {0}", ex.Message);
                passed = false;
            }

            return passed;
        }

        /// <summary>
        /// TODO - Should convert this to  Dictionary<TestItem_Enum, TestPoint> ...
        /// </summary>
        /// <param name="dSpectrumData"></param>
        /// <param name="fSpan"></param>
        /// <param name="iSteps"></param>
        /// <param name="fReceiverCenterFreq"></param>
        /// <param name="fSourceCenterFreq"></param>
        /// <param name="use_cal_data"></param>
        /// <param name="dut_input_cable_loss"></param>
        /// <param name="dut_output_cable_loss"></param>
        /// <param name="use_secondary_source_analyzer"></param>
        /// <param name="secondary_source_analyzer_config"></param>
        public void MeasureNPRP(ref double[][] dSpectrumData, double fSpan, int iSteps, double fReceiverCenterFreq, double fSourceCenterFreq, bool use_cal_data = false, double dut_input_cable_loss = 0, double dut_output_cable_loss = 0,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double dDUTInputLoss = 0.0;
            double dDUTOutputLoss = 0.0;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                double FFT_SPAN = 64.0e6;//sample rate 
                double VSG_STEP = 1e6; //1 MHz step        
                bool Overload = false;
                int fftLength = 64;
                double power = 0.0;
                double WINDOW_ENDW = .987; //Measured & Calculated effective noise BW of window shape uniform
                double MXA_ENBW = 1.059;  //Measured ENBW for MXA signal analyzer.  Used for correlation   
                double LOG_VIDEO_NOISE_AVG_ERROR = -2.51;
                double NORM_DET_NOISE_AVG_ERROR = 1.0;

                int numberPoint = (int)(fSpan * 1e6 / 1e6);

                double[] _1MData = new double[iSteps];

                GetCableLoss(ref dDUTInputLoss, ref dDUTOutputLoss, vsg.Frequency + vsg.BasebandFrequency, vsa.Frequency + vsa.PowerAcqOffsetFreq);

                if (vxt == null)
                {
                    vsg.Frequency = fSourceCenterFreq * 1e6;
                    vsa.Frequency = fReceiverCenterFreq * 1e6;
                    vsg.BlankRFDuringTuning = true;
                    //setup receiver for Noise power for Rx Path measurement
                    vsa.SetupReceiverForNPRP();
                    vsg.Apply();
                    vsa.Apply();

                    //vsg.Modulation.Enabled = true;  try     // enable modulated signal                                    
                    for (int i = 0; i < iSteps; i++) //Make 1 acquisition for each step
                    {
                        vsg.BasebandFrequency = (i * VSG_STEP);  //Step source using fast baseband tuning                         
                        vsg.Apply();
                        vsg.WaitUntilSettled(1000);
                        vsa.Arm();
                        vsa.ReadMagnitudeDataForNPRP(0, ref dSpectrumData[i], ref Overload);  //Read spectrum, place in array Data
                        if (Overload == true)  //Check for ADC Overload
                        {
                            Logging(LogLevel.INFO, "ADC Overload\n" + "Please check RF Input Power settings on VSA and try again");
                            break;
                        }
                    }

                    //restore Vsg status
                    vsg.BasebandFrequency = 0;  //Step source using fast baseband tuning                         
                    vsg.Apply();

                    // Process Data
                    PowerSolutionExt IntBW = new PowerSolutionExt(1e6, 1.05, 100, 71, null, true); //dummy data to start
                    IntBW.Interval = FFT_SPAN / fftLength;  //interval between data points
                    IntBW.Nbw = 1.0; //set to 1 because we use this in 1 MHz results
                    IntBW.Rbw = 1e6; //Current RBW
                    IntBW.Span = fSpan;            //Span for 1 MHz signal acquisition        
                    IntBW.NumPoints = numberPoint;
                    //find frequency points
                    //Data Processing
                    for (int i = 0; i < iSteps; i++)
                    {
                        //process 1 MHz rbw data
                        for (int j = (int)((fftLength - numberPoint) / 2); j <= dSpectrumData[i].Length - (int)((fftLength - numberPoint) / 2) - 1; j++)  //29/28 used to process 70 MHz span from 128 MHz.  29 points + 28 = 71 points
                        {
                            power = 10 * Math.Log10(dSpectrumData[i][j]); //calculate dBm from mW
                            power += (10 * Math.Log10(MXA_ENBW / WINDOW_ENDW)) + LOG_VIDEO_NOISE_AVG_ERROR + NORM_DET_NOISE_AVG_ERROR; //account for ENBW and error compensation from MXA AVG
                            power += dDUTOutputLoss;
                            dSpectrumData[i][j] = power;     //place in 2D array row [i] column [j]                            
                            _1MData[i] = power;
                        }

                    }

                }
                else
                {
                    vsg.Frequency = fSourceCenterFreq * 1e6;
                    vsa.Frequency = fReceiverCenterFreq * 1e6;

                    //setup receiver for Noise power for Rx Path measurement
                    vsa.SetupReceiverForNPRP();

                    //vsg.Modulation.Enabled = true;  try     // enable modulated signal                                    
                    for (int i = 0; i < iSteps; i++) //Make 1 acquisition for each step
                    {
                        vsg.BasebandFrequency = (i * VSG_STEP);  //Step source using fast baseband tuning                         
                        vxt.Apply();

                        vxt.ReadMagnitudeDataForNPRP(0, ref dSpectrumData[i], ref Overload);  //Read spectrum, place in array Data
                        if (Overload == true)  //Check for ADC Overload
                        {
                            Logging(LogLevel.INFO, "ADC Overload\n" + "Please check RF Input Power settings on VSA and try again");
                            break;
                        }
                    }

                    //restore vxt status
                    vsg.BasebandFrequency = 0;
                    vxt.Apply();

                    // Process Data
                    PowerSolutionExt IntBW = new PowerSolutionExt(1e6, 1.05, 100, 71, null, true); //dummy data to start
                    IntBW.Interval = FFT_SPAN / fftLength;  //interval between data points
                    IntBW.Nbw = 1.0; //set to 1 because we use this in 1 MHz results
                    IntBW.Rbw = 1e6; //Current RBW
                    IntBW.Span = fSpan;            //Span for 1 MHz signal acquisition        
                    IntBW.NumPoints = numberPoint;
                    //find frequency points
                    //Data Processing
                    for (int i = 0; i < iSteps; i++)
                    {
                        //process 1 MHz rbw data
                        for (int j = (int)((fftLength - numberPoint) / 2); j <= dSpectrumData[i].Length - (int)((fftLength - numberPoint) / 2) - 1; j++)  //29/28 used to process 70 MHz span from 128 MHz.  29 points + 28 = 71 points
                        {
                            power = 10 * Math.Log10(dSpectrumData[i][j]); //calculate dBm from mW
                            power += (10 * Math.Log10(MXA_ENBW / WINDOW_ENDW)) + LOG_VIDEO_NOISE_AVG_ERROR + NORM_DET_NOISE_AVG_ERROR; //account for ENBW and error compensation from MXA AVG
                            power += dDUTOutputLoss;
                            dSpectrumData[i][j] = power;     //place in 2D array row [i] column [j]                            
                            _1MData[i] = power;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "MeasureNPRP failed! {0}", ex.Message);
            }

        }

        /// <summary>
        /// Function to let AWG generate dynamic pulse
        /// </summary>
        /// <param name="playCachedWaveform">whether to play the cached arb or not</param>
        /// <param name="awgArb">the cached AWG arb name</param>
        /// <param name="waveform">the corresponding VSG arb name</param>
        /// <param name="dcLeadTime">lead time for dynamic pulse</param>
        /// <param name="dcLagTime">lag time for dynamic pulse</param>
        /// <param name="dutyCycle">duty cycle</param>
        /// <param name="pulseVoltage">pulse voltage</param>
        /// <param name="triggerLineNumber">PXI trigger line</param>
        /// <param name="dRFTriggerDelay">trigger delay</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        public void AwgGenDynamicPulse(bool playCachedWaveform, string awgArb, string waveform, double dcLeadTime, double dcLagTime, double dutyCycle, double pulseVoltage, int triggerLineNumber, double dRFTriggerDelay,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            //double triggerDelay = 343.94e-9;//delay calibration

            //if (vxt == null)
            //{
            //    triggerDelay = 343.94e-9;
            //}
            //else
            //{
            //    triggerDelay = 296.94e-9;
            //}

            try
            {
                //// Generate Pulse for PA Enable signal
                if (!playCachedWaveform)
                    awg.playPulseMode(triggerLineNumber, vsg.GetWaveformTime(waveform), dcLeadTime, dcLagTime, dutyCycle, pulseVoltage, dRFTriggerDelay, vxt != null);
                else
                {
                    awg.playCachedPulse(awgArb, pulseVoltage);
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "AwgGenDynamicPulse Failed! {0}", ex.Message);
            }

            //// Setup the VSG and VSA for waveform, band frequency and power level
            //vsg.setVsgIqDelay(0);

            //execute WLANEVM test
        }

        /// <summary>
        /// Function to Measure state transition time
        /// </summary>
        /// <param name="tps">test point definitions</param>
        /// <param name="r">the test result (state transition time)</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool MeasureStateTransition(Dictionary<TestItem_Enum, TestPoint> tps, ref double r, bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {

            bool ret = true;
            TestPoint t;
            Stopwatch sw = new Stopwatch();
            sw.Reset();

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            double[] IQData = new double[1];

            // 40MHz sampling rate, duration 10 us
            double sampling_rate = 100e6;
            double duration = 10e-6;

            if (vxt == null)
            {
                sampling_rate = 40e6;
            }
            else
            {
                sampling_rate = 100e6;
            }

            vsa.GetIQData(ref IQData, sampling_rate, duration);

            //test result calculation, according to test requirement, we need find out the samples that having 90% power of signal with stable power.
            //1. calculate the power with stable status, I choose 2us with 10 samples
            double dStartTimeWithStablePower = 1e-6;
            int iStartIndexWithStablePower = (int)(dStartTimeWithStablePower * sampling_rate);
            int iSampleAccumulateNumber = 10;
            double dAccumulatePower = 0.0;
            for (int i = iStartIndexWithStablePower; i < (iStartIndexWithStablePower + iSampleAccumulateNumber); i++)
            {
                dAccumulatePower += Math.Pow(IQData[2*i],2.0) + Math.Pow(IQData[2 * i+1], 2.0);
            }

            //double dStablePowerdB = 10 * Math.Log10(dAccumulatePower/ iSampleAccumulateNumber);
            double dStablePower = dAccumulatePower / iSampleAccumulateNumber;

            double dTargetPowerValue = dStablePower * 0.9;

            //search Time Point of sample that exceed the target power
            int index = 0;
            int iTotalNumberSamples = (int)(duration * sampling_rate);
            while (index < iTotalNumberSamples)
            {
                if ((Math.Pow(IQData[2*index],2.0)+ Math.Pow(IQData[2*index+1], 2.0)) >= dTargetPowerValue)
                {
                    break;
                }
                else
                {
                    index++;
                }
            }

            double dTimingOfTargetSample = index * (1.0 / sampling_rate);

            r = dTimingOfTargetSample;

            return ret;
        }



        #region Xapp
        public bool  xAppLaunch(bool xAppM900Enable, string suppTech,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                SUPPORTEDFORMAT tech_format = TestConditionSetup_GetFormat(suppTech);
                if (plugInVsaXapp != null)
                    plugInVsaXapp.LunchXapp(vsa.xAppAddress, xAppM900Enable, tech_format);
                else
                    plugInXapp.xAppLaunch(vsa.xAppAddress, null, xAppM900Enable, tech_format);
                
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
        public bool xAppMeasureEvm(string suppTech,
                                   xCommonSetting commonSetting,
                                   object formatSetting,
                                   ref xEvmResult result,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double cf;
            double PowerLevel;
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                cf = vsa.Frequency;
                PowerLevel = vsa.Power;
                SUPPORTEDFORMAT tech_format = SUPPORTEDFORMAT.CW;
                double bw = 5e6;
                MODULATIONTYPE modtype = MODULATIONTYPE.QPSK;
                DIRECTION direct = DIRECTION.UPLINK;

                TestConditionSetup_GetWaveformInfo(suppTech, ref tech_format, ref bw, ref modtype, ref direct);
                ret = plugInXapp.xAppMeasureEvm(tech_format, bw, direct, cf, PowerLevel,
                                    commonSetting, formatSetting,
                                    ref result);
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;

        }

        public bool xAppMeasureSem(string suppTech,
                                   xCommonSetting commonSetting,
                                   object formatSetting,
                                   ref xSemResult res,
                                   ref xSemResultPassFail resPass,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)

        {
            double cf;
            double PowerLevel;
            bool  ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                cf = vsa.Frequency;
                PowerLevel = vsa.Power;
                SUPPORTEDFORMAT tech_format = SUPPORTEDFORMAT.CW;
                double bw = 5e6;
                MODULATIONTYPE modtype = MODULATIONTYPE.QPSK;
                DIRECTION direct = DIRECTION.UPLINK;

                TestConditionSetup_GetWaveformInfo(suppTech, ref tech_format, ref bw, ref modtype, ref direct);
                ret = plugInXapp.xAppMeasureSem(tech_format, bw, direct, cf, PowerLevel, 
                                       commonSetting, formatSetting,
                                       ref res, ref resPass);
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        public bool xAppMeasureOrfs(string suppTech,
                                    xCommonSetting commonSetting,
                                    xOrfsSetting orfsSetting,
                                    ref OrfsxAppResult result,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double cf;
            double PowerLevel;
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                SUPPORTEDFORMAT tech_format = SUPPORTEDFORMAT.CW;
                double bw = 5e6;
                MODULATIONTYPE modtype = MODULATIONTYPE.QPSK;
                DIRECTION direct = DIRECTION.UPLINK;

                cf = vsa.Frequency;
                PowerLevel = vsa.Power;
                TestConditionSetup_GetWaveformInfo(suppTech, ref tech_format, ref bw, ref modtype, ref direct);

                ret = plugInXapp.xAppMeasureOrfs(tech_format, direct, cf, PowerLevel,
                                       commonSetting, orfsSetting,
                                       ref result);

                vsa.Frequency = cf;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }
        #endregion

        #region  KMF 
        /// <summary>
        /// Function to measure EVM - simulate hardware? (KMF) 
        /// </summary>
        /// <param name="power_range">the power range for VSA</param>
        /// <param name="selected_tech">selected technology</param>
        /// <param name="frequency">test frequency</param>
        /// <param name="search_length">search length for WLAN signal</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureEVM_HD(double power_range, string selected_tech, double frequency, double search_length, Dictionary<TestItem_Enum, TestPoint> tps,
                         bool use_secondary_source_analyzer = false,
                         SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double[] IQData = new double[1];
            double[] evmData = null;
            EVMSettings setting = new EVMSettings();
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                DIRECTION dir = DIRECTION.UPLINK;

                TestConditionSetup_GetWaveformInfo(selected_tech, ref setting.tech_format, ref setting.bandwidth, ref setting.mod_type, ref dir);
                setting.frequecy = frequency;
                setting.sampling_rate = vsa.INIInfo.EvmSamplingRate;

                if (setting.tech_format == SUPPORTEDFORMAT.TDSCDMA)
                    setting.td_enableHspa8Psk = vsa.INIInfo.EnableHSPA8psk_TD;
                if ((setting.tech_format == SUPPORTEDFORMAT.LTEFDD) ||
                    (setting.tech_format == SUPPORTEDFORMAT.LTETDD))
                {

                    setting.lte_analysisBoundary = vsa.INIInfo.EvmAnalysisBoundary;
                    setting.resultLength = vsa.INIInfo.EvmResultLength;
                }
                if ((setting.tech_format == SUPPORTEDFORMAT.WLAN11A) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11AC) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11AX) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11B) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11G) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11N) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11P))
                {
                    setting.search_length = search_length;
                }

                plugInKmf.ConfigureEvm(setting);


                evmData = plugInKmf.MeasureEVM_HD(ref ret, power_range);

                Logging(LogLevel.INFO, "Select_tech: {0}, frequency: {1}, KMF EVM {2} ", selected_tech, frequency.ToString(), evmData[0].ToString());
                sw.Stop();

                ret = setAndCheckResult(tps, TestItem_Enum.EVM, evmData[0], sw.ElapsedMilliseconds, Logging) && ret;

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "KmfMeasureEVM Failed! {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to Measure EVM (KMF)
        /// </summary>
        /// <param name="noise_reduction">whether apply noise reduction</param>
        /// <param name="selected_tech">selected technology</param>
        /// <param name="frequency">test frequency</param>
        /// <param name="search_length">search length for WLAN signal</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <param name="capture">whether to capture IQ data or use IQ data provided in the parameter</param>
        /// <param name="iq_data">the provided IQ data</param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureEVM(bool noise_reduction, string selected_tech, double frequency, double search_length, Dictionary<TestItem_Enum, TestPoint> tps,
                                bool use_secondary_source_analyzer = false,
                                SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
                                bool capture = true, double[] iq_data = null)
        {
            double[] IQData = new double[1];
            double[] evmData = null;
            EVMSettings setting = new EVMSettings();
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                DIRECTION dir = DIRECTION.UPLINK;

                TestConditionSetup_GetWaveformInfo(selected_tech, ref setting.tech_format, ref setting.bandwidth, ref setting.mod_type, ref dir);
                setting.frequecy = frequency;
                setting.sampling_rate = vsa.INIInfo.EvmSamplingRate;

                if (setting.tech_format == SUPPORTEDFORMAT.TDSCDMA)
                    setting.td_enableHspa8Psk = vsa.INIInfo.EnableHSPA8psk_TD;
                if ((setting.tech_format == SUPPORTEDFORMAT.LTEFDD) ||
                    (setting.tech_format == SUPPORTEDFORMAT.LTETDD))
                {

                    setting.lte_analysisBoundary = vsa.INIInfo.EvmAnalysisBoundary;
                    setting.resultLength = vsa.INIInfo.EvmResultLength;
                }
                if ((setting.tech_format == SUPPORTEDFORMAT.WLAN11A) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11AC) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11AX) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11B) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11G) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11N) ||
                    (setting.tech_format == SUPPORTEDFORMAT.WLAN11P))
                {
                    setting.search_length = search_length;
                }


                plugInKmf.ConfigureEvm(setting);

                if (capture == true)
                    vsa.GetIQData(ref IQData, vsa.INIInfo.EvmSamplingRate, vsa.INIInfo.EvmSamplingDuration);
                else
                    IQData = iq_data;

                if (noise_reduction == true)
                {
                    ret = plugInKmf.MeasureNoiseReductionIQData(ref IQData);
                    if (ret == true)
                    {
                        Logging(LogLevel.INFO, "Select_tech: {0}, noise reduction operation succeeded!", selected_tech);
                    }
                    else
                    {
                        Logging(LogLevel.ERROR, "Select_tech: {0}, noise reduction operation failed!", selected_tech);
                    }
                }

                evmData = plugInKmf.MeasureEVM(ref ret, IQData);

                Logging(LogLevel.INFO, "Select_tech: {0}, frequency: {1}, KMF EVM {2} ", selected_tech, frequency.ToString(), evmData[0].ToString());
                sw.Stop();

                ret = setAndCheckResult(tps, TestItem_Enum.EVM, evmData[0], sw.ElapsedMilliseconds, Logging) && ret;

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "KmfMeasureEVM Failed! {0}", ex.Message);
                ret = false;
            }
     
            return ret;
        }

        /// <summary>
        /// Function to Measure EVM for LTE-A (KMF)
        /// </summary>
        /// <param name="selected_tech">selected technology</param>
        /// <param name="frequency">test frequency</param>
        /// <param name="search_length">search length for WLAN signal</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <param name="capture">whether to capture IQ data or use IQ data provided in the parameter</param>
        /// <param name="iq_data">the provided IQ data</param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureLTEAEVM(string selected_tech, double frequency, double search_length, Dictionary<TestItem_Enum, TestPoint> tps,
                                    bool use_secondary_source_analyzer = false,
                                    SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY,
                                    bool capture = true, double[] iq_data = null)
        {
            double[] IQData = new double[1];
            double[] ComponentCarrierEvm = null;
            EVMSettings setting = new EVMSettings();
            bool ret = true;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                DIRECTION dir = DIRECTION.UPLINK;

                TestConditionSetup_GetWaveformInfo(selected_tech, ref setting.tech_format, ref setting.bandwidth, ref setting.mod_type, ref dir);
                setting.frequecy = frequency;
                setting.sampling_rate = vsa.INIInfo.EvmSamplingRate;

                setting.lte_analysisBoundary = vsa.INIInfo.EvmAnalysisBoundary;
                setting.resultLength = vsa.INIInfo.EvmResultLength;
       

                setting.carrier_count = vsa.INIInfo.CarrierCount;
                setting.cc_offsets = vsa.INIInfo.CCOffsets;


                plugInKmf.ConfigureEvm(setting);

                if (capture == true)
                    vsa.GetIQData(ref IQData, vsa.INIInfo.EvmSamplingRate, vsa.INIInfo.EvmSamplingDuration);
                else IQData = iq_data;

                //string filename = string.Format(@"C:\Sxq\{0}.csv", selected_tech);
                //saveCSVFile(filename, IQData);

                ComponentCarrierEvm = plugInKmf.MeasureLTEAEVM(ref ret, IQData);

                Logging(LogLevel.INFO, "Select_tech: {0}, frequency: {1}", selected_tech, frequency.ToString());

                for (int i= 0; i < vsa.INIInfo.CarrierCount; i++)
                {
                    Logging(LogLevel.INFO, "Carrier Number {0}, EVM: {1}", i, ComponentCarrierEvm[i].ToString());
                }
                sw.Stop();

                ret = setAndCheckEvmResults(tps, TestItem_Enum.EVM, ComponentCarrierEvm, sw.ElapsedMilliseconds, Logging) && ret;

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "KmfMeasureEVM Failed! {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to Measure SEM(KMF)
        /// </summary>
        /// <param name="user_setting">SEM settings</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="semResult">the SEM result</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureSEM(SemUserConfigure user_setting, 
                                  Dictionary<TestItem_Enum, TestPoint> tps,
                                  ref SEMResult semResult, 
                                  bool use_secondary_source_analyzer = false,
                                  SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double[] IQData = new double[1];

            bool ret = true;
            string strResult = string.Empty;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                user_setting.RrcFilterBandwidth = vsa.INIInfo.FilterBW;
                user_setting.RrcFilterAlpha = vsa.INIInfo.FilterAlpha;
                user_setting.carrier_count = vsa.INIInfo.CarrierCount;

                if (!user_setting.manual_range)
                    user_setting.range = vsa.Power;

                plugInKmf.ConfigureSem(user_setting);

                ret = plugInKmf.MeasureSEM(ref semResult);

                //Logging(LogLevel.INFO, "Select_tech: {0}, frequency: {1}, KMF EVM {2} ", selected_tech, frequency.ToString(), evmData[0].ToString());
                sw.Stop();

                //ret = ret && setAndCheckResult(tps, TestItem_Enum.KMF_EVM, evmData[0], sw.ElapsedMilliseconds, Logging);

            }
            catch (Exception ex)
            {
                semResult.errMsg = ex.Message;
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to Capture SEM Data
        /// </summary>
        /// <param name="user_setting">SEM settings</param>
        /// <param name="sem_data">the captured sem data</param>
        /// <param name="sample_rate">the sampling rate</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureSEM_Acq(SemUserConfigure user_setting,
                                  ref double[] sem_data,
                                  ref double sample_rate,
                                  bool use_secondary_source_analyzer = false,
                                  SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double[] IQData = new double[1];

            bool ret = true;
            string strResult = string.Empty;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                user_setting.RrcFilterBandwidth = vsa.INIInfo.FilterBW;
                user_setting.RrcFilterAlpha = vsa.INIInfo.FilterAlpha;
                user_setting.carrier_count = vsa.INIInfo.CarrierCount;

                if (!user_setting.manual_range)
                    user_setting.range = vsa.Power;

                plugInKmf.ConfigureSem(user_setting);

                ret = plugInKmf.MeasureSEM_Acq(ref sem_data, ref sample_rate);

                sw.Stop();
            }
            catch (Exception ex)
            {
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to calculate SEM based on previous data capture
        /// </summary>
        /// <param name="user_setting">SEM settings</param>
        /// <param name="tps">test points definitions</param>
        /// <param name="semResult">the SEM result</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureSEM_Calc(SemUserConfigure user_setting,
                          Dictionary<TestItem_Enum, TestPoint> tps,
                          ref SEMResult semResult,
                          bool use_secondary_source_analyzer = false,
                          SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            double[] IQData = new double[1];

            bool ret = true;
            string strResult = string.Empty;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();

                user_setting.RrcFilterBandwidth = vsa.INIInfo.FilterBW;
                user_setting.RrcFilterAlpha = vsa.INIInfo.FilterAlpha;
                user_setting.carrier_count = vsa.INIInfo.CarrierCount;

                if (!user_setting.manual_range)
                    user_setting.range = vsa.Power;

                plugInKmf.ConfigureSem(user_setting);

                ret = plugInKmf.MeasureSEM_Calc(vsa.IQInfo, ref semResult);

                if (ret == true) semResult.result = "Pass";
                else semResult.result = "Fail";

                sw.Stop();
            }
            catch (Exception ex)
            {
                semResult.errMsg = ex.Message;
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Function to measure ORFS for GSM
        /// </summary>
        /// <param name="user_setting">ORFS setting</param>
        /// <param name="tps">test point definitions</param>
        /// <param name="orfsResult">ORFS test result</param>
        /// <param name="use_secondary_source_analyzer">whether to use the 2nd source/analyzer</param>
        /// <param name="secondary_source_analyzer_config">the 2nd source/analyzer config <see cref="SECONDARY_SOURCE_ANALYZER_CONFIG"/> </param>
        /// <returns>true for success, false for fail</returns>
        public bool KmfMeasureORFS(OrfsUserConfigure user_setting,
                                   Dictionary<TestItem_Enum, TestPoint> tps,
                                   ref ORFSResult orfsResult,
                                  bool use_secondary_source_analyzer = false,
                                  SECONDARY_SOURCE_ANALYZER_CONFIG secondary_source_analyzer_config = SECONDARY_SOURCE_ANALYZER_CONFIG.VSA_ONLY)
        {
            bool ret = true;
            double dutInputLoss = 0;
            double dutOutputLoss = 0;
            bool overload = false;
            double measPout = 0;

            IVSG vsg = null;
            IVSA vsa = null;
            IVXT vxt = null;
            setup2ndSourceAnalyzer(ref vsg, ref vsa, ref vxt, use_secondary_source_analyzer, secondary_source_analyzer_config);

            try
            {
                Stopwatch sw = new Stopwatch();

                sw.Reset();
                sw.Start();
                GetCableLoss(ref dutInputLoss, ref dutOutputLoss,
                             user_setting.centreFrequency,
                             user_setting.centreFrequency);

                vsa.MeasurePOut(dutOutputLoss, ref overload, ref measPout, false);


                orfsResult.measPout = measPout;
                orfsResult.pcl = OrfsStandards.MeasPower2Pcl(user_setting.band, measPout);

                user_setting.power = OrfsStandards.Pcl2Power(user_setting.band, orfsResult.pcl);

                if (!user_setting.manual_range)
                    user_setting.range = vsa.Power;

                plugInKmf.ConfigureOrfs(user_setting);



                plugInKmf.MeasureORFS(ref ret, ref orfsResult);

                //Logging(LogLevel.INFO, "Select_tech: {0}, frequency: {1}, KMF ORFS {2} ", selected_tech, frequency.ToString(), evmData[0].ToString());
                sw.Stop();
                Logging(LogLevel.INFO, "MF ORFS time {0} ", sw.ElapsedMilliseconds);
                //ret = ret && setAndCheckResult(tps, TestItem_Enum.ORFS, orfsResult, sw.ElapsedMilliseconds, Logging);

            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, "KmfMeasureORFS Failed! {0}", ex.Message);
                ret = false;
            }

            return ret;
        }

        #endregion

        #region Vsa 89600

        /// <summary>
        /// Do Lte measurments in VSA 89600
        /// </summary>
        /// <param name="centerFreq" unit= MHz></param>
        /// <returns>evm value Lte signal</returns>
        public bool DoVsaMeasurements(bool IsPrimaryHwVxt, string suppTech, double centerFreq, ref double evmResult, bool doEvm = true, bool doIQMod = true, bool doSpectrum = true)
        {
            bool ret = true;
            try
            {
                SUPPORTEDFORMAT tech_format = SUPPORTEDFORMAT.CW;
                double bw = 5e6;
                MODULATIONTYPE modtype = MODULATIONTYPE.QPSK;
                DIRECTION dir = DIRECTION.DOWNLINK;     

                TestConditionSetup_GetWaveformInfo(suppTech, ref tech_format, ref bw, ref modtype, ref dir);

                if (vsa89600 != null)
                {
                    if (tech_format == SUPPORTEDFORMAT.LTEFDD)
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        ret = vsa89600.ConfigLteFddMeasurement(tech_format, bw, centerFreq / 1e3, IsPrimaryHwVxt);
                        sw.Stop();
                        if (ret == false)
                            return ret;
                        sw.Reset();
                        sw.Start();
                        evmResult = vsa89600.DoLteFddEvmMeasurements(doEvm, doIQMod, doSpectrum, ref ret);
                        sw.Stop();
                        if (ret == false)
                            return ret;
                        return true;
                    }
                    else if (tech_format == SUPPORTEDFORMAT._5GNR)
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        ret = vsa89600.Config5GNR_Measurement(tech_format, bw, centerFreq / 1e3, IsPrimaryHwVxt);
                        sw.Stop();
                        if (ret == false)
                            return ret;
                        sw.Reset();
                        sw.Start();
                        evmResult = vsa89600.Do5GNR_EvmMeasurements(doEvm, doIQMod, doSpectrum, ref ret);
                        sw.Stop();
                        if (ret == false)
                            return ret;
                        return true;
                    }
                    else
                    {
                        Logging(LogLevel.WARNING, "Current technology have not been supported by VSA 89600.");
                        return false;
                    }
                }
                else return false;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, ex.Message);
                return false;
            }
        }

        public void Restart89600()
        {
            try
            {
                vsa89600.Restart();
            }
            catch (Exception ex)
            {
            }
        }
        public bool Disconnect89600()
        {
            bool ret = true;

            try
            {
                vsa89600.Disconnect();
            }
            catch (Exception ex)
            {
                ret = false;
            }

            return ret;
        }

        #endregion

        #endregion
    }

    public class PlugInMeas_Pre5G : PlugInMeasBase
    {
        private IVSA Vsa;
        private IDigitizer Digitizer;
        private IVSG Vsg;
        private IAWG2 Awg;
        private PlugInVsa89600 vsa89600 = null;

        public PlugInMeas_Pre5G(IVSA vsa, IDigitizer digitizer, IVSG vsg, IAWG2 awg)
        {
            S890xA_LicenseInit();
            if (s8901a_licensed == false)
            {
                LogFunc(LogLevel.ERROR, "S8901A license not installed");
                return;
            }
            Vsa = vsa;
            Digitizer = digitizer;
            Vsg = vsg;
            Awg = awg;
        }

        public void SetVsa89600( PlugInVsa89600 vsa)
        {
            vsa89600 = vsa;
        }

        /// <summary>
        /// Do Pre5G measurments in VSA 89600
        /// </summary>
        /// <param name="centerFreq" unit= MHz></param>
        /// <returns>evm value of 8 carriers</returns>
        public bool DoVsaPre5GMeasurements(double centerFreq, double freq_offset,ref List<double> evmResult, bool doEvm = true, bool doIQMod = true, bool doSpectrum = true)
        {
            bool ret = true;
            try
            {                
                if (vsa89600 != null)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    ret = vsa89600.CreatePre5GMeasurement(centerFreq/1e3,freq_offset/1e3);
                    sw.Stop();
                    if (ret == false)
                        return ret;
                    sw.Reset();
                    sw.Start();
                    evmResult = vsa89600.DoPre5GEvmMeasurements(doEvm,doIQMod,doSpectrum);
                    sw.Stop();
                    return true;
                }
                else return false;
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, ex.Message);
                return false;
            }
        }
        public bool SetupReceiver(double freq, double power, double if_center, string channel, string trggerSrc)
        {
            bool ret = true;
            try
            {
                ret = (Vsa as PlugInM9393A_Pre5G).ConfigM9393(freq, power);

                ret = ret && (Vsa as PlugInM9393A_Pre5G).SetIFFilter(true, if_center);

                ret = ret && (Digitizer as PlugInM9203A).ConfigInput(channel, 1.0, 0,
                                                   AgMD2VerticalCouplingEnum.AgMD2VerticalCouplingDC,
                                                   true, false, 0, 0);
                ret = ret && (Digitizer as PlugInM9203A).ConfigTrigger(trggerSrc);
            }
            catch
            {
                ret = false;
            }
            return ret;
        }

        public bool Acquistion(string channel, double duration, ref float[] iq_data)
        {
            bool ret = true;
            double sample_rate = 1.6e9;
            long size = Convert.ToInt64(duration * sample_rate);

            try
            {
                (Digitizer as PlugInM9203A).ConfigAcquisition(1, size, sample_rate, 
                                            AgMD2AcquisitionModeEnum.AgMD2AcquisitionModeNormal);

                (Digitizer as PlugInM9203A).Acquisition();

                float[] if_data = null;
                (Digitizer as PlugInM9203A).AcquistionFetch(channel, size, 0, ref if_data);

                saveCSVFile(@"C:\\Temp\Record_Data.CSV", if_data);

                //(Digitizer as PlugInM9203A).AcquistionFetch(channel, size, 0, ref iq_data);
                //DDC:

                float[] fIQSampleArray = new float[2 * if_data.Length];
                Array.Clear(fIQSampleArray, 0, fIQSampleArray.Length);

                //case 1.2G
                // I -> Cos((1.2e9)/(1.6e9)*2*pi*i): 1,0,-1,0
                // Q -> -Sin((1.2e9)/(1.6e9)*2*pi*i):0,1,0,-1 
                // I: 0,-1,0,1---Q:1,0,-1,0
                //for (int i = 0; i < if_data.Length / 2; i++)
                //{
                //    fIQSampleArray[4 * i + 1] = ((i % 2) * (-2) + 1) * if_data[2 * i];  //for Q
                //    fIQSampleArray[4 * i + 2] = ((i % 2) * 2 - 1) * if_data[2 * i + 1]; //for I                
                //}

                //case 0.4G
                // I -> Cos((0.4e9)/(1.6e9)*2*pi*i): 1,0,-1,0
                // Q -> Sin((0.4e9)/(1.6e9)*2*pi*i):0,1,0,-1 
                // I: 0,-1,0,1---Q:1,0,-1,0
                for (int i = 0; i < if_data.Length / 2; i++)
                {
                    fIQSampleArray[4 * i + 1] =  (-1)*((i % 2) * (2) - 1) * if_data[2 * i];  //for Q
                    fIQSampleArray[4 * i + 2] = ((i % 2) * (2) - 1) * if_data[2 * i + 1]; //for I                
                }


                ////// I: 1,0,-1,0---Q:0,1,0,-1
                //////Sun it is also good
                //for (int i = 0; i < if_data.Length / 2; i++)
                //{
                //    fIQSampleArray[4 * i] = ((i % 2) * (-2) + 1) * if_data[2 * i];  //for I
                //    fIQSampleArray[4 * i + 3] = (-1)*((i % 2) * 2 - 1) * if_data[2 * i + 1]; //for Q                 
                //}


                iq_data = (float[])fIQSampleArray.Clone();

                saveCSVFile(@"C:\\Temp\Record_Data_DDC.CSV", iq_data);

            }
            catch
            {
                ret = false;
            }
            return ret;
        }

        private void saveCSVFile(string fileName, float[] data)
        {

            try
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);

                StreamWriter sw = new StreamWriter(fileName, true);

                for (int i = 0; i < data.Length; i++)
                {
                    sw.WriteLine(data[i].ToString());
                }

                sw.Close();
            }
            catch
            { }

        }
        private bool SetupUpConverter(double freq, double power)
        {
            try
            {
                if (Vsg != null && Vsg.GetType() == typeof(PlugInRfSigGen))
                {
                    Vsg.WideBandEnabled = true;
                    Vsg.ALCEnabled = false;
                    Vsg.ModulationEnabled = true;
                    Vsg.Frequency = freq;
                    Vsg.Amplitude = power;
                    Vsg.OutputEnabled = true;
                    return true;
                }
                else
                {
                    Logging(LogLevel.ERROR, "Incorrect Source");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool LoadWfmWaveformAndPlay(string waveform, string aliasName,double sampleRate)
        {
            bool ret = true;
            try
            {
                if (Awg != null)
                {
                    if (Awg.GetType() == typeof(PlugInM9336A))
                    {
                        //  ret = Awg.ClearMemory();

                        ret = Awg.LoadWaveform(waveform, aliasName, "Channel1,Channel2");
                        if (ret == false)
                            return ret;

                        ret = Awg.SetSampleRate(ChannelEnum.Channel1,sampleRate);
                        if (ret == false)
                            return ret;

                        ret = Awg.SetSampleRate(ChannelEnum.Channel2, sampleRate);
                        if (ret == false)
                            return ret;


                        ret = Awg.SetChannelEnable(ChannelEnum.Channel1, true);
                        if (ret == false)
                            return ret;

                        ret = Awg.SetChannelEnable(ChannelEnum.Channel2, true);
                        if (ret == false)
                            return ret;

                        ret = Awg.PlayWaveformWithIQChannels();
                        if (ret == false)
                            return ret;

                        return true;
                    }

                    else if (Awg.GetType() == typeof(PlugInM8195A))
                    {
                        ret = Awg.ClearMemory();

                        ret = Awg.LoadIQWaveforminDualMode(waveform);
                        if (ret == false)
                            return ret;

                        //Set Amplitude and Offset for M8195A
                        Awg.SetChannelAmplitude(1, 6.0E-01);
                        Awg.SetChannelAmplitude(4, 6.0E-01);
                        Awg.SetChannelOffSet(1, 0.0);
                        Awg.SetChannelOffSet(4, 0.0);

                        ret = Awg.SetSampleRate(sampleRate);
                        if (ret == false)
                            return ret;

                        ret = Awg.PlayIQWaveform(1, 4);
                        if (ret == false)
                            return ret;
                        return true;
                    }
                    else if (Awg.GetType() == typeof(PlugInM8190A))
                    {
                        ret = Awg.ClearMemory();

                        ret = Awg.LoadIQWaveforminDualMode(waveform);
                        if (ret == false)
                            return ret;

                        //Set Amplitude and Offset for M8190A
                        Awg.SetChannelAmplitude(1, 6.0E-01);
                        Awg.SetChannelAmplitude(2, 6.0E-01);
                        Awg.SetChannelOffSet(1, 0.0);
                        Awg.SetChannelOffSet(2, 0.0);

                        ret = Awg.SetSampleRate(sampleRate);
                        if (ret == false)
                            return ret;

                        ret = Awg.PlayIQWaveform(1, 2);
                        if (ret == false)
                            return ret;
                        return true;
                    }
                    
                    else
                    {
                        Logging(LogLevel.ERROR, "Incorrect Awg");
                        return false;
                    }
                }

                else
                {
                    Logging(LogLevel.ERROR, "Incorrect Awg");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, ex.Message);
                return false;
            }

        }

        
        /// <summary>
        /// Setup5G Source PSG + AWG , play wfm file
        /// </summary>
        /// <param name="freq"></param>
        /// <param name="power"></param>
        /// <param name="waveform"></param>
        /// <param name="aliasName"></param>
        /// <param name="sampleRate"></param>
        /// <returns></returns>
        public bool Setup5GSource(double freq, double power, string waveform, string aliasName, double sampleRate)
        {
            bool ret = true;

            try
            {
                ret = SetupUpConverter(freq, power);
                if (ret == false)
                    return ret;

                ret = LoadWfmWaveformAndPlay(waveform, aliasName,sampleRate);
                if (ret == false)
                    return ret;

                return true;                
            }
            catch (Exception ex)
            {
                Logging(LogLevel.ERROR, ex.Message);
                return false;
            }
        }
        
    }
}

