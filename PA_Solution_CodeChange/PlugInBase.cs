using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;

using Ivi.Visa.Interop;

namespace Keysight.S8901A.Common
{

    #region Enumeration Definitions

    public enum CW_WAVEFORM_TYPE
    {
        CW = 0,
        CW_POWER_RAMP = 1,
        CW_TWO_TONE = 2,
        CW_AND_MODULATION = 3
    }

    public enum ChannelEnum
    {
        [Description("Channel1")]
        Channel1 = 1,
        [Description("Channel2")]
        Channel2 = 2,
        [Description("Channel3")]
        Channel3 = 3
    }
    public enum ACPR_FORMAT_TYPE
    {
        [Description("Standard")]
        ACPR_STANDARD_FORMAT = 0,
        [Description("LTE")]
        ACPR_LTE_FORMAT = 1,
        [Description("GSM")]
        ACPR_GSM_FORMAT = 2
    }

    
    public enum SUPPORTEDTECHNOLOGIES
    {
        CW,
        GSM,
        EDGE,
        EVDO,
        WCDMA,
        CDMA2000,
        LTE5MHZ,
        LTE10MHZ,
        LTE20MHZ,
        LTE20_20MHZ,
        LTE5_5MHZ,
        LTETDD5MHZ,
        LTETDD10MHZ,
        LTETDD20MHZ,
        TDSCDMA,
        WLANN20MHZ,
        WLANN40MHZ,
        WLANAC20MHZ,
        WLANAC40MHZ,
        WLANAC80MHZ,
        WLANAC160MHZ,
        WLANAX20MHZ,
        WLANAX40MHZ,
        WLANAX80MHZ,
        WLANAX160MHZ,
        WLANA20MHZ,
        WLANB20MHZ,
        WLANG20MHZ,
        WLANJ10MHZ,
        WLANJ20MHZ,
        WLANP5MHZ,
        WLANP10MHZ,
        WLANP20MHZ,
        _5GNRBW1,
        _5GNRBW2
    };
    

    public enum SUPPORTEDFORMAT
    {
        CW,
        GSM,
        EDGE,
        EVDO,
        WCDMA,
        CDMA2000,
        TDSCDMA,
        LTEFDD,
        LTETDD,
        LTEAFDD,
        LTEATDD,
        WLAN11N,
        WLAN11AC,
        WLAN11AX,
        WLAN11A,
        WLAN11B,
        WLAN11G,
        WLAN11J,
        WLAN11P,
        _5GNR
    }

    public enum DIRECTION
    {
        UPLINK,
        DOWNLINK
    };

    public enum TRIGGERMODE
    {
        IMMEDIATE,
        EXTENAL
    };

    public enum MODULATIONTYPE
    {
        BPSK,
        QPSK,
        QAM16,
        QAM64,
        QAM256,
        QAM1024
    };

    public enum BANDWIDTH
    {
        BW_5MHZ,
        BW_10MHZ,
        BW_20MHZ,
        BW_40MHZ,
        BW_80MHZ,
        BW_160MHZ
    };


   
    public enum ruiORFSTYPE
    {
        MODulation,
        MSWitching,
        SWITching,
        FFModulation
    };

    public enum P2PTransferType
    {
        VSG_TO_MACC, MACC_TO_VSG, VSA_TO_MACC, MACC_TO_AWG
    };

    public enum UserFileType 
    {
        [Description("Signal Studio")]
        SignalStudio,
        [Description("MATLAB Complex")]
        MatlabComplex,
        [Description("Big Endian Int16")]
        BigEndianInt16,
        [Description("Text / CSV")]
        TextCsv
    };

    public enum WaveformSourceType
    {
        [Description("Pre-Defined Waveform")]
        PreDefined,
        [Description("User-Defined Waveform")]
        UserDefined,
    }
    public enum UserDefinedModelType
    {
        [Description("MATLAB Script")]
        Matlab_Script,
        [Description("User-Defined Function")]
        UserDefined_Function,
    }


    public enum EnvInputType { 
        NORMALIZED_IQ, ABSOLUTE_RF, OVERRIDE_ABS_RF 
    };

    public enum CfrAlgorithm
    {
        PEAK_WINDOWING = 0,
        CLIPING_FILTERING = 1,
        PEAK_CANCELLATION = 2
    }

    public enum DpdModelType 
    { 
        LUT = 0,
        MEMORY,
        VOLTERA,
        USER_DEFINED 
    };

    public enum LogLevel
    {
        ERROR   = 0,
        WARNING = 1,
        INFO    = 2,
        DEBUG   = 3
    };

    public enum MeasurementPlatform
    {
        KMF, XAPP
    };

    public enum TestItem_Enum
    {
        NONE,

        PA_GAIN,
        POUT,
        PIN,

        ACPR_L1,   // 1st adjacent channel, Lower (For LTE, UTRAN 1st adjacent channel)
        ACPR_H1,   // 1st adjacent channel, Higher (For LTE, UTRAN 1st adjacent channel)
        ACPR_L2,   // 2nd adjacent channel, Lower (For LTE, UTRAN 2st adjacent channel)
        ACPR_H2,   // 2nd adjacent channel, Higher (For LTE, UTRAN 2st adjacent channel)
        ACPR_L3,   // for LTE, E-UTRAN 1st adjacent channel, Lower
        ACPR_H3,   // for LTE, E-UTRAN 1st adjacent channel, Higher

        ORFS_L1,   // 1st adjacent channel 
        ORFS_H1,   // 1st adjacent channel 
        ORFS_L2,   // 2nd adjacent channel
        ORFS_H2,   // 2nd adjacent channel

        HARMONICS_2, //2nd Harmonic
        HARMONICS_3, //3rd Harmonic
        HARMONICS_4, //4th Harmonic
        HARMONICS_5, //5th Harmonic

        ICC,
        IBAT,
        ICUSTOM1,
        ICUSTOM2,
        ITOTAL,
        PAE,

        GAIN_COMP_1DB,
        GAIN_COMP_2DB,
        GAIN_COMP_3DB,
        COMP_1DB_INPOWER,
        COMP_2DB_INPOWER,
        COMP_3DB_INPOWER,
        LINEAR_GAIN,

        IMD3_LOW,
        IMD3_HIGH,
        IIP3_LOW,
        IIP3_HIGH,

        EVM,
        ORFS,
        SEM,
        DYNAMIC_EVM,
        DELTA_EVM,

        S_PARAMETER,

        NPRP, // Noise Power for Rx Path
        NF,    // Noise Figure
        PPMU_VOLT,
        PPMU_CURR,
        STATE_TRANS,

       /* Below items are used by MeasurementDataCommunication ,please don't change it*/
        ET,  //ET
        DPD,
        ET_ACPR1,
        ET_ACPR2,
        DPD_PRE,
        DPD_POST,
        /*End MeasurementDataCommunication*/
    }

    public enum PXIChassisTriggerEnum
    {
        // The PXI Chassis Trigger Enum value match the source and analyzer's TriggerSource Enum value
        PXIChassisTrigger0 = 0,
        PXIChassisTrigger1 = 1,
        PXIChassisTrigger2 = 2,
        PXIChassisTrigger3 = 3,
        PXIChassisTrigger4 = 4,
        PXIChassisTrigger5 = 5,
        PXIChassisTrigger6 = 6,
        PXIChassisTrigger7 = 7
    }

    public enum AnalyzerFFTWindowShapeEnum
    {
        FFTWindowShapeUniform = 0,
        FFTWindowShapeHann = 1,
        FFTWindowShapeFlatTop = 2,
        FFTWindowShapeHDRFlatTop = 3,
        FFTWindowShapeGaussian = 4
    }

    public enum AnalyzerFFTAcquisitionLengthEnum
    {
        FFTAcquisitionLength_64 = 64,
        FFTAcquisitionLength_128 = 128,
        FFTAcquisitionLength_256 = 256,
        FFTAcquisitionLength_512 = 512
    }

    public enum AnalyzerAcquisitionModeEnum
    {
        AcquisitionModeIQ = 0,
        AcquisitionModeSpectrum = 1,
        AcquisitionModePower = 2,
        AcquisitionModeFFT = 3
    }

    public enum AnalyzerConversionEnum
    {
        ConversionSingleHighSide = 0,
        ConversionSingleLowSide = 1,
        ConversionImageProtect = 2,
        ConversionAuto = 3
    }

    public enum AnalyzerMemoryModeEnum
    {
        MemoryModeNormal = 0,
        MemoryModeLargeAcquisition = 1
    }

    public enum AnalyzerTriggerModeEnum
    {
        AcquisitionTriggerModeImmediate = 0,
        AcquisitionTriggerModeExternal = 1,
        AcquisitionTriggerModeMagnitude = 2,
        AcquisitionTriggerModeWidebandMagnitude = 3,
        AcquisitionTriggerModeSoftware = 4,
        AcquisitionTriggerModeScheduler = 5,
    }

    public enum AnalyzerTriggerEnum
    {
        TriggerFrontPanelTrigger1 = 0,
        TriggerFrontPanelTrigger2 = 1,
        TriggerPXITrigger0 = 2,
        TriggerPXITrigger1 = 3,
        TriggerPXITrigger2 = 4,
        TriggerPXITrigger3 = 5,
        TriggerPXITrigger4 = 6,
        TriggerPXITrigger5 = 7,
        TriggerPXITrigger6 = 8,
        TriggerPXITrigger7 = 9,
    }

    public enum AnalyzerTriggerTimeoutModeEnum
    {
        TriggerTimeoutModeWaitInfinite = 0,
        TriggerTimeoutModeTimeoutAbort = 1,
        TriggerTimeoutModeAutoTriggerOnTimeout = 2,
    }

    public enum AnalyzerChannelFilterShapeEnum
    {
        ChannelFilterShapeNone = 0,
        ChannelFilterShapeRectangular = 1,
        ChannelFilterShapeGaussian = 2,
        ChannelFilterShapeNyquist = 3,
        ChannelFilterShapeRaisedCosine = 4,
        ChannelFilterShapeRootRaisedCosine = 5,
        ChannelFilterShapeRootNyquist = 6
    }

    public enum AnalyzerSampleSizeEnum
    {
        SampleSize32Bits = 0,
        SampleSize64Bits = 1,
    }

    public enum AnalyzerIQUnitsEnum
    {
        IQUnitsSquareRootMilliWatts = 0,
        IQUnitsVolts = 1,
    }

    public enum AnalyzerPowerUnitsEnum
    {
        PowerUnitsVoltsSquared = 0,
        PowerUnitsdBm = 1,
        PowerUnitsdBmPerHz = 2,
    }

    public enum AnalyzerAlignmentTypeEnum
    {
        AnalyzerAlignmentTypeComprehensive = 0,
        AnalyzerAlignmentTypeIFFilters = 1,
        AnalyzerAlignmentTypeLONulling = 2,
        AnalyzerAlignmentTypeAmplitudeCurrentState = 3,
        AnalyzerAlignmentTypeAmplitudeAllStates = 4,
        AnalyzerAlignmentTypeAllRecommended = 5,
        AnalyzerAlignmentTypeLOLevel = 6,
        AnalyzerAlignmentTypeReferenceAmplitude = 7,
        AnalyzerAlignmentTypeReconfiguration = 8,
        AnalyzerAlignmentTypePeriodic = 9,
        AnalyzerAlignmentTypeQuick = 10
    }

    public enum Source_MarkerEnum
    {
        Marker_None = 0,
        Marker_1 = 1,
        Marker_2 = 2,
        Marker_3 = 3,
        Marker_4 = 4
    }

    public enum Source_SynchronizationTriggerTypeEnum
    {
        SynchronizationTriggerTypePerArb = 0,
        SynchronizationTriggerTypePerSequenceStep = 1,
        SynchronizationTriggerTypePerSequence = 2,
        SynchronizationTriggerTypeDataMarker = 3,
    }

    public enum Source_SynthesizerPLLModeEnum
    {
        SynthesizerPLLModeNormal = 0,
        SynthesizerPLLModeBestWideOffset = 1
    }

    public enum PortEnum
    {
        PortRFInput = 0,
        PortRFOutput = 1,
        PortRFIOHD = 2,
        PortRFIOFD = 3,
    }

    public enum SourceTriggerNumberEnum
    {
        SourceTrigger1 = 0,
        SourceTrigger2 = 1,
        SourceTrigger3 = 2,
    }

    public enum SourceTriggerModeEnum
    {
        SourceTriggerModeLevel = 0,
        SourceTriggerModePulse = 1,
    }

    public enum SourceTriggerPolarityEnum
    {
        SourceTriggerPolarityPositive = 0,
        SourceTriggerPolarityNegative = 1,
    }

    public enum SourceTriggerEnum
    {
        SourceTriggerFrontPanelTrigger = 0,
        SourceTriggerInternalTrigger = 1,
        SourceTriggerPXITrigger0 = 2,
        SourceTriggerPXITrigger1 = 3,
        SourceTriggerPXITrigger2 = 4,
        SourceTriggerPXITrigger3 = 5,
        SourceTriggerPXITrigger4 = 6,
        SourceTriggerPXITrigger5 = 7,
        SourceTriggerPXITrigger6 = 8,
        SourceTriggerPXITrigger7 = 9,
    }

    public enum PXI_CHASSIS_TRIGGER_BUS_CONFIG
    {
        ISOLATE_ALL,
        SEG1_TO_SEG2,
        SEG2_TO_SEG1,
        SEG1_TO_SEG2_TO_SEG3,
        SEG3_TO_SEG2_TO_SEG1,
        SEG2_TO_SEG3,
        SEG3_TO_SEG2,
        SEG2_TO_SEG1_SEG3
    }

    public enum VIO_STATE
    {
        VIO_STATE_OFF = 0,
        VIO_STATE_ON = 1
    }

    public enum PRIMARY_SOURCE_ANALYZER_CONFIG
    {
        VSA_AND_VSG = 0,
        VXT = 1
    }

    public enum SECONDARY_SOURCE_ANALYZER_CONFIG
    {
        NONE = 0,
        VSA_ONLY = 1
        /*
        VSG_ONLY = 2,
        VSG_AND_VSA = 3,
        VXT = 4
        */
    }

    public enum PPMU_OPERATION
    {
        FORCE_VOLTAGE = 0,
        FORCE_CURRENT = 1,
        FORCE_VOLTAGE_MEASURE_CURRENT = 2,
        FORCE_VOLTAGE_MEASURE_VOLTAGE = 3,
        FORCE_CURRENT_MEASURE_VOLTAGE = 4,
        FORCE_CURRENT_MEASURE_CURRENT = 5,
        FORCE_NOTHING_MEASURE_VOLTAGE = 6,
        HIGH_IMPEDANCE = 7
    }

    public enum FEM_STATE_TRANSITION
    {
        TX_to_RXLNA_Transition = 0,
        TX_to_RXBYP_Transition = 1,
        RXBYP_to_RXLNA_Transition = 2,
        RXLNA_to_TX_Transition = 3
    }


    public enum VNA_PORT_NUM_CONFIG
    {
        TWO_PORT = 2,
        THREE_PORT = 3,
        FOUR_PORT = 4,
        FIVE_PORT = 5,
        SIX_PORT = 6,
    }

    public enum IM3_RESULT_FORMAT
    {
        IMD3 = 0,
        IIP3
    }

    public enum IM3_WAVEFORM_FORMAT
    {
        CW_TWO_TONE = 0,
        CW_AND_MODULATION
    }

    public enum HARMONICS_RESULT_FORMAT
    {
        DBC_PER_CHANNEL = 0,
        DBM_PER_MHZ
    }

    #endregion

    #region Class Definitions

    public abstract class PlugInBase : xAppDomainBase
    {

        Queue<object> stackLog = new System.Collections.Generic.Queue<object>();
        bool bUseExternalQueueForLogging = false;

        public delegate void LogData(LogLevel lvl, string message, params object[] args);
        public LogData logData = null;
        public bool logEnabled = true;


        public void Logging(LogLevel lvl, string message, params object[] args)
        {
            if (bUseExternalQueueForLogging == false)
            {
                if (logEnabled && logData != null)
                    logData(lvl, message, args);
            }
            else
            {
                StringBuilder sb = new StringBuilder().AppendFormat(message, args);
                stackLog.Enqueue(sb.ToString());
            }

        }

        protected virtual void LogFunc(LogLevel lvl, string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        //public object GetLogObject()
        //{
        //    if (stackLog.Count != 0)
        //        return stackLog.Dequeue();
        //    else
        //        return null;
        //}
        //public void SetLogObject( ref Queue<object> refQueue )
        //{
        //    if (this.stackLog != null)
        //    {
        //        this.stackLog = null;
        //        bUseExternalQueueForLogging = true;
        //    }
        //    this.stackLog = refQueue;
        //}
        //public Queue<object> GetLog()
        //{
        //    return stackLog;
        //}
        //public void ClearLog()
        //{
        //    stackLog.Clear();
        //}

        protected ResourceManager rmXapp = null;
        protected FormattedIO488 ioXapp = null;


        protected static bool AboutEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }
        protected static void CopyFillComplexArray(float[] src, float[] dst)
        {
            // Copy src array to dst
            Array.Copy(src, dst, Math.Min(src.Length, dst.Length));

            int count = (dst.Length - src.Length) / 2;  // complex pairs
            if (count <= 0)
                return;

            // Zero-hold last complex value to fill the destination array
            int index = src.Length;
            float r = src[index - 2];
            float i = src[index - 1];
            while (--count >= 0)
            {
                dst[index++] = r;
                dst[index++] = i;
            }
        }
        protected static float[] convertToFloat(double[] inputArray)
        {
            if (inputArray == null)
                return null;

            float[] output = new float[inputArray.Length];
            for (int i = 0; i < inputArray.Length; i++)
                output[i] = (float)inputArray[i];

            return output;
        }

    }

    public class DPDETResult
    {
        public double InitialIQ;
        public double FinalIQ;
        public double PreEVM;
        public double PostEVM;
        public double TotalTime;
        public double IQAlignTime;
        public double ModelExtractingTime;
        public double TotalDPDTime;
        public string strMessage;
        public DPDETResult()
        {
        }
        public DPDETResult( double InitIQ, double finalIQ, double preEVM, double postEVM, double TotTime, double iqaTime, double modelExtTime, double totDPDTime)
        {
            InitialIQ = InitIQ;
            FinalIQ = finalIQ;
            PreEVM = preEVM;
            PostEVM = postEVM;
            TotalTime = TotTime;
            IQAlignTime = iqaTime;
            ModelExtractingTime = modelExtTime;
            TotalDPDTime = totDPDTime;
            strMessage = InitialIQ.ToString() + "/" +
                      FinalIQ.ToString() + "/" +
                      preEVM.ToString() + "/" +
                      postEVM.ToString() + "/" +
                      TotalTime.ToString() + "/" +
                      IQAlignTime.ToString() + "/" +
                      ModelExtractingTime.ToString() + "/" +
                      TotalDPDTime.ToString() + "/";
        }
        public DPDETResult( string Message )
        {
            this.strMessage = Message;
        }
    }

    public class PowerServoResult
    {
        public string strMessage;
        public double dMeasuredPowerAtReceiver;
        public bool   bOverloaded;
        public bool   bServoPass;
        public int    nServoCount;
        public double dDUTInputLoss;
        public double dDUTOutputLoss;
        public double dFrequency;
        public double dDUTGain;
        public double dTolerance;
        public double dTargetPower;
        public double dTimeMeasured;
        public double dTimeACPR;
        public double dTimeSetupReceiver;
        public double dTimeSetupSource;
        public double dTimeSetupReceiverFreq;
        public double dTimeSetupSourceFreq;
        public double dActualSourceOutputPower;

        public bool bTestACPR = false;
        public bool bTestHarmonics = false;
        public bool bTestEVM = false;
        public bool bTestPVT = false;

        public double[] dACPRResult;
        public double dACPRSetupTime;
        public double dACPRTestTime;
        public double[] dHarmonicsResult;
        public double dHarmonicsSetupTime;
        public double dHarmonicsTestTime;
        public double dEVMResult;
        public double dEVMSetupTime;
        public double dEVMTestTime;
        public double[] dPVTResult;
        public double dPVTSetupTime;
        public double dPVTTestTime;

        public PowerServoResult()
        {
            strMessage = "NONE";
        }
        public PowerServoResult( string Message )
        {
            strMessage = Message;
        }
        public PowerServoResult(double MeasuredPowerAtreceiver,
                                bool Overloaded,
                                bool ServoPass,
                                int ServoCount,
                                double DUTInputLoss,
                                double DUTOutputLoss,
                                double Frequency,
                                double DUTGain,
                                double Tolerance,
                                double TargetPower,
                                int IterationCount,
                                double TimeMeasured,
                                double TimePerCycle,
                                double TimeACPR,
                                double TimeSetupReceiver,
                                double TimeSetupSource,
                                double TimeSetupReceiverFreq,
                                double TimeSetupSourceFreq,
                                double ActualSourceOutputPower,
                                string Message = "NONE")
        {
            dMeasuredPowerAtReceiver = MeasuredPowerAtreceiver;
            bOverloaded = Overloaded;
            bServoPass = ServoPass;
            nServoCount = ServoCount;
            dDUTInputLoss = DUTInputLoss;
            dDUTOutputLoss = DUTOutputLoss;
            dFrequency = Frequency;
            dDUTGain = DUTGain;
            dTolerance = Tolerance;
            dTargetPower = TargetPower;
            dTimeMeasured = TimeMeasured;
            dACPRTestTime = TimeACPR;
            dTimeSetupReceiver = TimeSetupReceiver;
            dTimeSetupSource = TimeSetupSource;
            dTimeSetupReceiverFreq = TimeSetupReceiverFreq;
            dTimeSetupSourceFreq = TimeSetupSourceFreq;
            dActualSourceOutputPower = ActualSourceOutputPower;
            strMessage = Message;

        }

        public void SavePowerServoResult(double MeasuredPowerAtreceiver,
                                bool Overloaded,
                                bool ServoPass,
                                int ServoCount,
                                double DUTInputLoss,
                                double DUTOutputLoss,
                                double Frequency,
                                double DUTGain,
                                double Tolerance,
                                double TargetPower,
                                int IterationCount,
                                double TimeMeasured,
                                double TimePerCycle,
                                double TimeACPR,
                                double TimeSetupReceiver,
                                double TimeSetupSource,
                                double TimeSetupReceiverFreq,
                                double TimeSetupSourceFreq,
                                double ActualSourceOutput,
                                string Message = "NONE")
        {
            dMeasuredPowerAtReceiver = MeasuredPowerAtreceiver;
            bOverloaded = Overloaded;
            bServoPass = ServoPass;
            nServoCount = ServoCount;
            dDUTInputLoss = DUTInputLoss;
            dDUTOutputLoss = DUTOutputLoss;
            dFrequency = Frequency;
            dDUTGain = DUTGain;
            dTolerance = Tolerance;
            dTargetPower = TargetPower;
            dTimeMeasured = TimeMeasured;
            dACPRTestTime = TimeACPR;
            dTimeSetupReceiver = TimeSetupReceiver;
            dTimeSetupSource = TimeSetupSource;
            dTimeSetupReceiverFreq = TimeSetupReceiverFreq;
            dTimeSetupSourceFreq = TimeSetupSourceFreq;
            dActualSourceOutputPower = ActualSourceOutput;
            strMessage = Message;

        }

        public void SaveEVMResult( double EVMSetupTime, double EVMTestTime, double EVMResult)
        {
            this.bTestEVM = true;
            this.dEVMSetupTime = EVMSetupTime;
            dEVMTestTime = EVMTestTime;
            dEVMResult = EVMResult;
            //return true;
        }

        public void SaveHarmonicsResult(double[] HarmonicsResult, double HarmonicsSetupTime, double HarmonicsTestTime)
        {
            this.bTestHarmonics = true;
            dHarmonicsResult = new double[HarmonicsResult.Length];
            Array.Copy(HarmonicsResult, dHarmonicsResult, HarmonicsResult.Length);
            dHarmonicsSetupTime = HarmonicsSetupTime;
            dHarmonicsTestTime = HarmonicsTestTime;
        }

        public void SavePVTResult(double[] PVTResult, double PVTSetupTime, double PVTTestTime)
        {
            this.bTestPVT = true;
            dPVTResult = new double[PVTResult.Length];
            Array.Copy(PVTResult, dPVTResult, PVTResult.Length);
            dPVTSetupTime = PVTSetupTime;
            dPVTTestTime = PVTTestTime;
        }
        public void SaveACPRResult( double[] ACPRResult, double ACPRSetupTime, double ACPRTestTime )
        {
            this.bTestACPR = true;
            this.dACPRResult = new double[ACPRResult.Length];
            Array.Copy(ACPRResult, dACPRResult, ACPRResult.Length);
            this.dACPRSetupTime = ACPRSetupTime;
            this.dACPRTestTime = ACPRTestTime;
        }
    }

    public class GainCompressionResult
    {
        public double linearGain;
        public double gainComp1dB;
        public double inPower1dB;
        public double gainComp2dB;
        public double inPower2dB;
        public double gainComp3dB;
        public double inPower3dB;
        public double maxPout;
        public double maxGainComp;
        public bool bGainCompressionPass;
    }

    public class INIAccess : xAppDomainBase
    {
        public string INIFile { get; set; }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        public int readNum;
        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public INIAccess(string file)
        {
            INIFile = file;
            FileInfo myFile = new FileInfo(INIFile);
            myFile.IsReadOnly = false;
        }
        public INIAccess()
        {
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            long lval = WritePrivateProfileString(Section, Key, Value, this.INIFile);
        }
        public void IniWriteIntArrayValue(string Section, string Key, int[] intDataArray)
        {
            string Value = "";
            for (int i = 0; i < intDataArray.Length; i++)
            {
                Value += intDataArray[i].ToString();
                if (i != (intDataArray.Length - 1))
                    Value += ",";
            }
            WritePrivateProfileString(Section, Key, Value, this.INIFile);
        }
        public void IniWriteComplexArrayValue(string Section, string Key, Complex[] complexDataArray)
        {
            string Value = "";
            for (int i = 0; i < complexDataArray.Length; i++)
            {
                string strT = "";
                if (complexDataArray[i].Imaginary < 0)
                {
                    if (complexDataArray[i].Imaginary == -1)
                    {
                        strT = complexDataArray[i].Real.ToString() + "-j";
                    }
                    else
                    {
                        strT = complexDataArray[i].Real.ToString() + complexDataArray[i].Imaginary.ToString() + "j";
                    }
                }
                else
                {
                    if (complexDataArray[i].Imaginary == 1)
                    {
                        strT = complexDataArray[i].Real.ToString() + "+j";
                    }
                    else if (complexDataArray[i].Imaginary == 0)
                    {
                        strT = complexDataArray[i].Real.ToString();
                    }
                    else
                    {
                        strT = complexDataArray[i].Real.ToString() + "+" + complexDataArray[i].Imaginary.ToString() + "j";
                    }
                }
                Value += strT;
                if (i != (complexDataArray.Length - 1))
                    Value += ",";
            }

            WritePrivateProfileString(Section, Key, Value, this.INIFile);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(32767);
            readNum = GetPrivateProfileString(Section, Key, "", temp,
                                            32767, this.INIFile);
            if (readNum < 1)
            {
                return "";
                //                throw new Exception("String reading at Section[" + Section + "] / Key[" + Key + "] reading failure!");
            }
            return temp.ToString();
        }
        public int IniReadIntValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(32767);
            readNum = GetPrivateProfileString(Section, Key, "", temp,
                                            32767, this.INIFile);
            if (readNum < 1)
            {
                return 0;
                //                throw new Exception("Integer reading at Section[" + Section + "] / Key[" + Key + "] reading failure!");
            }
            return Convert.ToInt32(temp.ToString());
        }
        public double IniReadDoubleValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(32767);
            readNum = GetPrivateProfileString(Section, Key, "", temp,
                                            32767, this.INIFile);
            if (readNum < 1)
            {
                return -9999;
                //                throw new Exception("Double reading at Section[" + Section + "] / Key[" + Key + "] reading failure!");
            }
            return Convert.ToDouble(temp.ToString());
        }
        public int[] IniReadIntArrayValue(string Section, string Key, char splitDelimeter)
        {
            StringBuilder temp = new StringBuilder(32767);
            int[] retIntArray = new int[1];
            readNum = GetPrivateProfileString(Section, Key, "", temp, 32767, this.INIFile);
            try
            {
                if (readNum < 1)
                {
                    throw new Exception("Integer Array reading at Section[" + Section + "] / Key[" + Key + "] reading failure!");
                }
                string[] strSplit = temp.ToString().Split(splitDelimeter);
                if (strSplit.Length < 2)
                {
                    throw new Exception("Integer Array reading at Section[" + Section + "] / Key[" + Key + "] reading failure! Length is less than 2");
                }
                retIntArray = new int[strSplit.Length];
                for (int i = 0; i < retIntArray.Length; i++)
                {
                    retIntArray[i] = Convert.ToInt32(strSplit[i]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retIntArray;
        }
        public Complex[] IniReadComplexArrayValue(string Section, string Key, char splitDelimeter)
        {
            StringBuilder temp = new StringBuilder(32767);
            Complex[] retComplexArray = new Complex[1];

            readNum = GetPrivateProfileString(Section, Key, "", temp, 32767, this.INIFile);
            try
            {
                if (readNum < 1)
                {
                    throw new Exception("Complex Array reading at Section[" + Section + "] / Key[" + Key + "] reading failure!");
                }
                string[] strSplit = temp.ToString().Split(splitDelimeter);
                if (strSplit.Length < 2)
                {
                    throw new Exception("Complex Array reading at Section[" + Section + "] / Key[" + Key + "] reading failure! Length is less than 2");
                }

                /*
                 * 1.먼저 첫 부호를 검색한다. 
                 * 2. '-' 이면 real을 -로 설정한다. 
                 * 3. '-'를 제거한다. 
                 * 4. 다시 '-'를 검색한다. 
                 * 5. '-'가 또 있으면 imaginary를 -로 한다. 
                 * 6. '+'를 검색한다. 
                 * 7. 있으면 Imaginary를 +로 한다. 
                 * 8. 둘 다 없으면 j를 검색한다. 
                 * 9. 없으면 Imaginary를 0으로 설정한다. 
                 * 10. 요걸 끝까지 반복한다. 
                 */
                retComplexArray = new Complex[strSplit.Length];
                int nCount = 0;
                foreach (string value in strSplit)
                {
                    string strValue = value;
                    string[] splitStr = null;
                    bool bFirstIsMinus = false;
                    bool bSecondIsMinus = false;
                    double real = 0, imag = 0;
                    if (strValue[0] == '-')
                    {
                        // 첫번째 값이 음수.. 실수인지 허수인지는 나중에...
                        bFirstIsMinus = true;
                        strValue = strValue.Remove(0, 1);
                    }
                    splitStr = strValue.Split('+');
                    if (splitStr.Length < 2)
                    {
                        splitStr = strValue.Split('-');
                        bSecondIsMinus = true;// 허수부가 음수.
                    }
                    if (splitStr.Length < 2) // 여전히 한개 이면..
                    {
                        // 실수인지 허수인지 찾는다. 
                        if (strValue.IndexOf('j') == -1) //허수 표시가 없으면..
                        {
                            // 실수
                            if (bFirstIsMinus)
                                real = Convert.ToDouble(strValue) * -1;
                            else
                                real = Convert.ToDouble(strValue);
                            imag = 0;
                            retComplexArray[nCount++] = new Complex(real, imag);
                        }
                        else //아니면
                        {
                            // 허수
                            strValue = strValue.Remove(strValue.Length - 1, 1);
                            if (strValue.Length == 0)
                                strValue = "1";
                            if (bFirstIsMinus)
                                imag = Convert.ToDouble(strValue) * -1;
                            else
                                imag = Convert.ToDouble(strValue);
                            real = 0;
                            retComplexArray[nCount++] = new Complex(real, imag);
                        }
                    }
                    else
                    {
                        real = Convert.ToDouble(splitStr[0]);
                        if (bFirstIsMinus)
                            real *= -1;
                        if (splitStr[1].Remove(splitStr[1].Length - 1, 1).Length == 0)
                            splitStr[1] = "1j";
                        imag = Convert.ToDouble(splitStr[1].Remove(splitStr.Length - 1, 1));

                        if (bSecondIsMinus)
                            imag *= -1;
                        retComplexArray[nCount++] = new Complex(real, imag);
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retComplexArray;
        }

        public static List<string> IniReadAllSection(string file)
        {
            TextReader iniFile = null;
            string strLine = null;
            List<string> sections = new List<string> ();

            if (File.Exists(file))
            {
                try
                {
                    iniFile = new StreamReader(file);
                    strLine = iniFile.ReadLine();
                    while (strLine != null)
                    {
                        strLine = strLine.Trim().ToUpper();
                        if (strLine != "")
                        {
                            if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                            {
                                string s = strLine.Substring(1, strLine.Length - 2);
                                sections.Add(s);
                            }
                        }
                        strLine = iniFile.ReadLine();
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (iniFile != null)
                        iniFile.Close();
                }
            }
            else
                throw new FileNotFoundException("Unable to locate " + file);

            return sections;
        }
    }

    public class SystemInfomration : INIAccess
    {

        string strSystemParameterSetupPath;
        string strSystemParameterSetupFile;

        public string strM9420AResourceString;
        public string strM938XResourceString;
        public string strM939XResourceString;
        //public string strMMACResourceString;
        public double ServoTargetPOut;
        public double ServoExpectedGain;
        public double ServoPoutTolerance;
        public string strTestFrequency;
        public double InputLoss;
        public double OutputLoss;

        public SystemInfomration()
        {
            this.strSystemParameterSetupPath = Directory.GetCurrentDirectory();
            this.strSystemParameterSetupFile = strSystemParameterSetupPath + @"\Systemsetup\system.ini";
            this.INIFile = strSystemParameterSetupFile;
            GetSystemInformation();
        }

        void GetSystemInformation()
        {
            try
            {
                strM9420AResourceString = this.IniReadValue("system", "M9420A visa address");
                if (strM9420AResourceString == "") throw new Exception();
            }
            catch
            {
                strM9420AResourceString = "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR";
                IniWriteValue("system", "M9420A VISA Address", "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR");
            }

            try
            {
                strM938XResourceString = this.IniReadValue("system", "M938xA visa address");
                if (strM938XResourceString == "") throw new Exception();
            }
            catch
            {
                strM938XResourceString = "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR";
                IniWriteValue("system", "M938xA VISA Address", "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR");
            }

            try
            {
                strM939XResourceString = this.IniReadValue("system", "M939xA visa address");
                if (strM939XResourceString == "") throw new Exception();
            }

            catch
            {
                strM939XResourceString = "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR";
                IniWriteValue("system", "M939xA VISA Address", "PXI0::CHASSIS1::SLOT2::FUNC0::INSTR");
            }



            try
            {
                ServoTargetPOut = IniReadDoubleValue("Servo Setup", "Target Pout");
                if (ServoTargetPOut == -9999) throw new Exception();
            }
            catch
            {
                ServoTargetPOut = 0;
                IniWriteValue("Servo Setup", "Target Pout", "0.0");
            }

            try
            {
                ServoExpectedGain = IniReadDoubleValue("Servo Setup", "Expected Gain");
                if (ServoExpectedGain == -9999) throw new Exception();

            }
            catch
            {
                ServoExpectedGain = 10;
                IniWriteValue("Servo Setup", "Expected Gain", "10.0");
            }

            try
            {
                ServoPoutTolerance = IniReadDoubleValue("Servo Setup", "Pout Tolerance");
                if (ServoPoutTolerance == -9999) throw new Exception();
            }
            catch
            {
                ServoPoutTolerance = 0.1;
                IniWriteValue("Servo Setup", "Pout Tolerance", "0.1");
            }

            try
            {
                strTestFrequency = IniReadValue("Servo Setup", "Test Frequency");
                if (strTestFrequency == "") throw new Exception();
            }
            catch
            {
                strTestFrequency = "1700,1800,1900";
                IniWriteValue("Servo Setup", "Test Frequency", strTestFrequency);
            }

            try
            {
                InputLoss = IniReadDoubleValue("Servo Setup", "Input Loss");
                if (InputLoss == -9999) throw new Exception();
            }
            catch
            {
                InputLoss = 6;
                IniWriteValue("Servo Setup", "Input Loss", "6");
            }

            try
            {
                OutputLoss = IniReadDoubleValue("Servo Setup", "Output Loss");
                if (OutputLoss == -9999) throw new Exception();
            }
            catch
            {
                OutputLoss = 0.5;
                IniWriteValue("Servo Setup", "Output Loss", "0.5");
            }
        }

        public void SetSystemInformation(string strInstrumentInfo, string TestFreq, string POut, string ExpGain, string Tolerance, string inputLoss, string outputLoss)
        {
            FileInfo myFile = new FileInfo(INIFile);
            myFile.IsReadOnly = false;
            string[] splitStr = strInstrumentInfo.Split(',');
            if( splitStr.Length > 0 )
                IniWriteValue("system", "M9420A visa address", splitStr[0]);
            if( splitStr.Length > 1 )
                IniWriteValue("system", "M938xA visa address", splitStr[1]);
            if (splitStr.Length > 2)
                IniWriteValue("system", "M939xA visa address", splitStr[2]);

            IniWriteValue("Servo Setup", "Target Pout", POut);
            IniWriteValue("Servo Setup", "Expected Gain", ExpGain);
            IniWriteValue("Servo Setup", "Pout Tolerance", Tolerance);
            IniWriteValue("Servo Setup", "Test Frequency", TestFreq);
            IniWriteValue("Servo Setup", "Input Loss", inputLoss);
            IniWriteValue("Servo Setup", "Output Loss", outputLoss);
        }
    }

    /*
    public class EVMMeasurementSetting
    {
        [CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("Sampling Rate for IQ Digitization. It should be same or greater than the RF Setup's Sampling Rate which is shown in PowerServo Tab")]
        public double SampleRate { get; set; }
        [CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("Acquistion Time. Time should be same or longer than waveform's playtime.")]
        public double AcquisitionTime { get; set; }
        //[CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("")]
        public double CenterFrequency;// { get; set; }
        //[CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("")]
        public double PowerLevel;// { get; set; }
        [CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("Trigger Delay.")]
        public double TriggerDelay { get; set; }
        [CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("Direction.")]
        public DIRECTION direction { get; set; }
        //[CategoryAttribute("EVM with Measurement Library"), DescriptionAttribute("Adding Offset to the head of IQTrace")]
        //public bool AddOffset { get; set; }
        [CategoryAttribute("LTE Only"), DescriptionAttribute("")]
        public bool UseOneSlotWaveform { get; set; }

        public EVMMeasurementSetting()
        {
            SampleRate = 1250000;
            AcquisitionTime = 1.2e-3;
            CenterFrequency = 900;
            TriggerDelay = -50e-6;
            //PowerLevel = 0;
            UseOneSlotWaveform = true;
            direction = DIRECTION.UPLINK;
            //this.AddOffset = false;
        }
        public EVMMeasurementSetting(SUPPORTEDTECHNOLOGIES suppTech)
        {
            switch (suppTech)
            {
                case SUPPORTEDTECHNOLOGIES.EDGE:
                    SampleRate = 1250000;
                    AcquisitionTime = 577e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                case SUPPORTEDTECHNOLOGIES.TDSCDMA:
                    SampleRate = 5e6;
                    AcquisitionTime = 577e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                case SUPPORTEDTECHNOLOGIES.WCDMA:
                    SampleRate = 5e6;
                    AcquisitionTime = 577e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                case SUPPORTEDTECHNOLOGIES.LTE5MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD5MHZ:
                    SampleRate = 5.625e6;
                    AcquisitionTime = 500e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                case SUPPORTEDTECHNOLOGIES.LTE10MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD10MHZ:
                    SampleRate = 11.25e6;
                    AcquisitionTime = 500e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                case SUPPORTEDTECHNOLOGIES.LTE20MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD20MHZ:
                    SampleRate = 22.5e6;
                    AcquisitionTime = 500e-6;
                    CenterFrequency = 900;
                    TriggerDelay = -50e-6;
                    PowerLevel = 0;
                    break;
                    //case SUPPORTEDTECHNOLGIES.LTETDD5MHZ:
                    //    SampleRate = 1250000;
                    //    FrameTime = 577e-6;
                    //    CenterFrequency = 900;
                    //    TriggerDelay = -50e-6;
                    //    PowerLevel = 0;
                    //    break;
                    //case SUPPORTEDTECHNOLGIES.LTETDD10MHZ:
                    //    SampleRate = 1250000;
                    //    FrameTime = 577e-6;
                    //    CenterFrequency = 900;
                    //    TriggerDelay = -50e-6;
                    //    PowerLevel = 0;
                    //    break;
                    //case SUPPORTEDTECHNOLGIES.LTETDD20MHZ:
                    //    SampleRate = 1250000;
                    //    FrameTime = 577e-6;
                    //    CenterFrequency = 900;
                    //    TriggerDelay = -50e-6;
                    //    PowerLevel = 0;
                    //    break;
            }
        }
    }
    */

    public class DCSMUInfo
    {
        [Flags]
        public enum SMU_CHANTYPE : uint
        {
            CHAN_FOR_VCC = 1,
            CHAN_FOR_VBAT = 2,
            CHAN_CUSTOMER_1 = 4,
            CHAN_CUSTOMER_2 = 8
        }

        public enum DCSMUTriggerSource
        {
            Immediate = 0,
            PXITrigger0 = 1,
            PXITrigger1 = 2,
            PXITrigger2 = 3,
            PXITrigger3 = 4,
            PXITrigger4 = 5,
            PXITrigger5 = 6,
            PXITrigger6 = 7,
            PXITrigger7 = 8
        }

        public double dVoltSetup;
        public double dCurrentSetup;
        public bool bMeasureCurrent;
        public bool bMeasureDC;
        public bool bEnabled;
        public int logicalChanIdx;
        public DCSMUTriggerSource triggerSource;
        public int triggerOffset;
        public int MeasurementDuration;
        public SMU_CHANTYPE chanType;
    }

    public class DIOInfo
    {
        public int rffeAddress;
        public int rffeRegister;
        public int rffeData;
        public bool rffeReadback;
        public int rffeReadData;
        public bool rffeRegCheck;
        public int rffeExpectedValue;
        public string rffeCommand;
        public DIOInfo()
        {
        }
        public DIOInfo( int Addr, int Reg, int Data, string Cmd, bool Readback, bool RegCheck, int ExpectedValue )
        {
            rffeAddress = Addr;
            rffeRegister = Reg;
            rffeData = Data;
            rffeReadback = Readback;
            //rffeReadData = ReadData;
            rffeCommand = Cmd;
            rffeRegCheck = RegCheck;
            rffeExpectedValue = ExpectedValue;
        }            
    }

    public class SwitchMatrix
    {
        public enum PortType_Enum
        {
            RF_PORT = 0,
            DC_PORT = 1
        }

        public class Port
        {
            public int id { get; set; }
            public string desc { get; set; }
            public PortType_Enum type { get; set; }
            public Port()
            {
                id = 0;
                desc = string.Empty;
                type = PortType_Enum.RF_PORT;
            }
            public Port(int identifier, string description = "", PortType_Enum port_type = PortType_Enum.RF_PORT)
            {
                id = identifier;
                desc = description;
                type = port_type;
            }
        }

        public class Connection
        {
            public int id { get; set; }
            public string desc { get; set; }
            public Port inst_port { get; set; }
            public Port dut_port { get; set; }
            public Connection()
            {
                id = 0;
                desc = null;
                inst_port = null;
                dut_port = null;
            }
            public Connection(int i, Port ip, Port dp, string d = "")
            {
                id = i;
                inst_port = ip;
                dut_port = dp;
                desc = d;
            }

        }

        public class Path
        {
            public int id { get; set; }
            public string desc { get; set; }
            public List<Connection> conns { get; set; }
            public Path()
            {
                conns = new List<Connection>();
            }
            public Path(int i, List<Connection> c, string d = "")
            {
                id = i;
                conns = c;
                desc = d;
            }
            public bool AddConnection(Connection c)
            {
                if (conns == null)
                    return false;

                var conn = from r in conns
                           where (r.inst_port.id == c.inst_port.id && r.dut_port.id == c.dut_port.id)
                           select r;
                if (conn.Count() == 0)
                {
                    conns.Add(c);
                }
                else
                {
                    Console.WriteLine("Connection already exists in conns!");
                }
                return true;
            }
            public bool RemoveConnection(Connection c)
            {
                var conn = from r in conns
                           where (r.inst_port.id == c.inst_port.id && r.dut_port.id == c.dut_port.id)
                           select r;
                if (conn.Count() > 0)
                {
                    conns.Remove(conn.First());
                    return true;
                }
                else
                    return false;
            }
        }

        public List<Port> inst_ports { get; set; }
        public List<Port> dut_ports { get; set; }
        public List<Connection> conns { get; set; }
        public List<Path> paths { get; set; }

        public string config_filename { get; set; }

        public void ReadFromConfigFile(string fileName)
        {
            XmlDocument xd = new XmlDocument();
            xd.Load(fileName);

            foreach (XmlNode x in xd.DocumentElement)
            {
                #region Instrument Port
                if (x.Name.Equals("InstrumentPorts"))
                {
                    foreach (XmlNode y in x.ChildNodes)
                    {
                        if (y.Name.Equals("Port"))
                        {
                            int i = int.Parse(y.Attributes["Id"].Value);
                            string s = y.Attributes["Description"].Value;
                            string t = y.Attributes["Type"].Value;
                            PortType_Enum type = (t.Equals("RF")) ? PortType_Enum.RF_PORT : PortType_Enum.DC_PORT;
                            inst_ports.Add(new Port(i, s, type));
                        }
                    }
                }
                #endregion

                #region Dut Port
                else if (x.Name.Equals("DutPorts"))
                {
                    foreach (XmlNode y in x.ChildNodes)
                    {
                        if (y.Name.Equals("Port"))
                        {
                            int i = int.Parse(y.Attributes["Id"].Value);
                            string s = y.Attributes["Description"].Value;
                            string t = y.Attributes["Type"].Value;
                            PortType_Enum type = (t.Equals("RF")) ? PortType_Enum.RF_PORT : PortType_Enum.DC_PORT;
                            dut_ports.Add(new Port(i, s, type));
                        }
                    }
                }
                #endregion

                #region Connections
                else if (x.Name.Equals("Connections"))
                {
                    foreach (XmlNode y in x.ChildNodes)
                    {
                        if (y.Name.Equals("Connection"))
                        {
                            int i = int.Parse(y.Attributes["Id"].Value);
                            string s = y.Attributes["Description"].Value;
                            int j = int.Parse(y.Attributes["InstPort"].Value);
                            int k = int.Parse(y.Attributes["DutPort"].Value);

                            var p1 = from r in inst_ports where r.id == j select r;
                            var p2 = from r in dut_ports where r.id == k select r;
                            if (p1.Count() > 0 && p2.Count() > 0)
                            {
                                Connection conn = new Connection(i, p1.First(), p2.First(), s);
                                conns.Add(conn);
                            }
                            else
                            {
                                throw (new Exception("Invalid Connection!"));
                            }
                        }
                    }
                }
                #endregion

                #region Paths
                else if (x.Name.Equals("Paths"))
                {
                    foreach (XmlNode y in x.ChildNodes)
                    {
                        if (y.Name.Equals("Path"))
                        {
                            int i = int.Parse(y.Attributes["Id"].Value);
                            string s = y.Attributes["Description"].Value;
                            List<Connection> lc = new List<Connection>();

                            foreach (XmlNode z in y.ChildNodes)
                            {
                                if (z.Name.Equals("Connection"))
                                {
                                    int k = int.Parse(z.Attributes["Id"].Value);
                                    var c = from r in conns
                                            where r.id == k
                                            select r;
                                    lc.Add(c.First());
                                }
                            }

                            paths.Add(new Path(i, lc, s));
                        }
                    }
                }

                #endregion
            }
        }

        public void SaveToConfigFile(string fileName)
        {
            XmlDocument xmldoc = new XmlDocument();

            XmlDeclaration xmldecl;
            xmldecl = xmldoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmldoc.AppendChild(xmldecl);

            XmlElement swConfig = xmldoc.CreateElement("SwitchConfiguration");
            xmldoc.AppendChild(swConfig);

            XmlElement xmlelementInstPort = xmldoc.CreateElement("InstrumentPorts");
            swConfig.AppendChild(xmlelementInstPort);

            foreach (Port p in inst_ports)
            {
                XmlElement elePort = xmldoc.CreateElement("Port");
                elePort.SetAttribute("Id", p.id.ToString());
                elePort.SetAttribute("Description", p.desc);
                elePort.SetAttribute("Type", p.type.ToString());
                xmlelementInstPort.AppendChild(elePort);
            }

            XmlElement xmlelementDutPort = xmldoc.CreateElement("DutPorts");
            swConfig.AppendChild(xmlelementDutPort);

            foreach (Port p in dut_ports)
            {
                XmlElement elePort = xmldoc.CreateElement("Port");
                elePort.SetAttribute("Id", p.id.ToString());
                elePort.SetAttribute("Description", p.desc);
                elePort.SetAttribute("Type", p.type.ToString());
                xmlelementDutPort.AppendChild(elePort);
            }

            XmlElement xmlelementConnection = xmldoc.CreateElement("Connections");
            swConfig.AppendChild(xmlelementConnection);

            foreach (Connection p in conns)
            {
                XmlElement eleConn = xmldoc.CreateElement("Connection");
                eleConn.SetAttribute("Id", p.id.ToString());
                eleConn.SetAttribute("Description", p.desc);
                eleConn.SetAttribute("InstPort", p.inst_port.id.ToString());
                eleConn.SetAttribute("DutPort", p.dut_port.id.ToString());
                xmlelementConnection.AppendChild(eleConn);
            }

            XmlElement xmlelementPath = xmldoc.CreateElement("Paths");
            swConfig.AppendChild(xmlelementPath);

            foreach (Path p in paths)
            {
                XmlElement elePath = xmldoc.CreateElement("Path");
                elePath.SetAttribute("Id", p.id.ToString());
                elePath.SetAttribute("Description", p.desc);
                foreach (Connection c in p.conns)
                {
                    XmlElement subele = xmldoc.CreateElement("Connection");
                    subele.SetAttribute("Id", c.id.ToString());
                    elePath.AppendChild(subele);
                }
                xmlelementPath.AppendChild(elePath);
            }

            xmldoc.Save(fileName);
        }


        public SwitchMatrix()
        {
            inst_ports = new List<Port>();
            dut_ports = new List<Port>();
            conns = new List<Connection>();
            paths = new List<Path>();
        }

        public SwitchMatrix(string filename)
        {
            inst_ports = new List<Port>();
            dut_ports = new List<Port>();
            conns = new List<Connection>();
            paths = new List<Path>();
            config_filename = filename;
            if (File.Exists(config_filename))
                ReadFromConfigFile(config_filename);
        }

        public SwitchMatrix(List<Port> dp, List<Port> ip, List<Connection> c, List<Path> p)
        {
            inst_ports = ip;
            dut_ports = dp;
            conns = c;
            paths = p;
        }

        public List<string> GetPathNames()
        {
            List<string> ls = new List<string>();
            foreach (Path p in paths)
            {
                ls.Add(p.desc);
            }
            return ls;
        }
        public Path SelectPath(string p)
        {
            var v = from r in paths where r.desc.Equals(p) select r;
            // Actions to turn switch to the correct position for the select Path
            if (v.Count() > 0)
                return v.First();
            else
                return null;
        }
        public Path SelectPath(int id)
        {
            var v = from r in paths where r.id == id select r;
            // Actions to turn switch to the correct position for the select Path
            if (v.Count() > 0)
                return v.First();
            else
                return null;
        }
    }

    [Serializable]
    public class TestParameter : INIAccess
    {
        public string[] INI_Properties =
        {
            "format",
            "bandwidth",
            "modulation",
            "direction",
            "waveform",
            "waveform_2",
            "vrampMask",
            "acprOffsetFreq",
            "ifBandwidth",
            "sampleRate",
            "pwrDuration",
            "acprDuration",
            "FilterType",
            "FilterAlpha",
            "FilterBw",
            "acprFilterType",
            "acprFilterBw",
            "acprSpan",
            "refLevelMargin",
            "digLevelOffset",
            "specHarmsSpan",
            "specHarmsRbw",
            "specHarmsAvgTime",
            "pvtBandwidth",
            "pvtSweepTime",
            "pvtMarkers",
            "evmSamplingRate",
            "enableHSPA8psk",
            "evmAnalysisBoundary",
            "evmResultLength",
            "carrierCount",
            "ccOffsets"
        };

        #region INI file defined fields
        public string Format;
        public string Bandwidth;
        public string Modulation;
        public string Direction;
        public string vrampMask;
        public double SampleRate;
        public double PowerDuration;
        public double AcprDuration;
        public double AcprFilterBW;
        public double AcprFilterAlpha;
        public AnalyzerChannelFilterShapeEnum AcprFilterType;
        public double AcprSpan;
        public int AcprAveragingNum;
        public double ReferenceLevelMargin;
        public double DigitalLevelOffset;
        public double[] AcprOffsetFrequencies;
        public double FilterAlpha;
        public double FilterBW;
        public double IFBandwidth;
        public double SpecHarmsSpan;
        public double SpecHarmsRBW;
        public double SpecHarmsAvgTime;
        public double PvtBandwidth;
        public double PvtSweepTime;
        public AnalyzerChannelFilterShapeEnum FilterType;
        public double[] PvtMarkers;

        public double EvmSamplingRate;
        public double EvmSamplingDuration;
        public bool EnableHSPA8psk_TD;
        public int EvmResultLength;
        public LteAnalysisBoundaryType EvmAnalysisBoundary;
        public int CarrierCount;
        public double[] CCOffsets;
        #endregion

        public string waveformPath { get; set; }
        public string waveformName { get; set; }
        public string waveformName2 { get; set; }
        public string technology { get; set; }
        public double waveformLength { get; set; }

        public TestParameter()
        {
            //if (strRFParameterFilePath == null)
            //{
            //    strRFParameterFilePath = Directory.GetCurrentDirectory() + @"\PowerServosetup\FPARAMETERSETUP.ini";
            //    this.path = strRFParameterFilePath;
            //    strWaveformPath = @".\waveform\";
            //}
            INIFile = null;
            waveformPath = null;
            waveformName = null;
            waveformName2 = null;
        }

        public AnalyzerChannelFilterShapeEnum convertFilterType(string strFilter)
        {
            switch (strFilter.ToUpper())
            {
                case "NONE":
                    return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeNone;
                case "RECTANGULAR":
                    return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeRectangular;
                case "GAUSSIAN":
                    return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeGaussian;
                case "RAISEDCOSINE":
                    return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeRaisedCosine;
                case "ROOTCOSINE":
                    return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeRootRaisedCosine;
            }
            return AnalyzerChannelFilterShapeEnum.ChannelFilterShapeNone;

        }

        /*
        public SUPPORTEDTECHNOLOGIES GetWaveformTechnology(string waveform)
        {
            switch (waveform)
            {
                case "CW": return SUPPORTEDTECHNOLOGIES.CW;
                case "GSM": return SUPPORTEDTECHNOLOGIES.GSM;
                case "EDGE": return SUPPORTEDTECHNOLOGIES.EDGE;
                case "EVDO": return SUPPORTEDTECHNOLOGIES.EVDO;
                case "WCDMA": return SUPPORTEDTECHNOLOGIES.WCDMA;
                case "CDMA2000": return SUPPORTEDTECHNOLOGIES.CDMA2000;
                case "LTE5MHZ": return SUPPORTEDTECHNOLOGIES.LTE5MHZ;
                case "LTE10MHZ": return SUPPORTEDTECHNOLOGIES.LTE10MHZ;
                case "LTE20MHZ": return SUPPORTEDTECHNOLOGIES.LTE20MHZ;
                case "LTE20_20MHZ": return SUPPORTEDTECHNOLOGIES.LTE20_20MHZ;
                case "LTETDD5MHZ": return SUPPORTEDTECHNOLOGIES.LTETDD5MHZ;
                case "LTETDD10MHZ": return SUPPORTEDTECHNOLOGIES.LTETDD10MHZ;
                case "LTETDD20MHZ": return SUPPORTEDTECHNOLOGIES.LTETDD20MHZ;
                case "TDSCDMA": return SUPPORTEDTECHNOLOGIES.TDSCDMA;
                case "WLANN20MHZ": return SUPPORTEDTECHNOLOGIES.WLANN20MHZ;
                case "WLANN40MHZ": return SUPPORTEDTECHNOLOGIES.WLANN40MHZ;
                case "WLANAC20MHZ": return SUPPORTEDTECHNOLOGIES.WLANAC20MHZ;
                case "WLANAC40MHZ": return SUPPORTEDTECHNOLOGIES.WLANAC40MHZ;
                case "WLANAC80MHZ": return SUPPORTEDTECHNOLOGIES.WLANAC80MHZ;
                case "WLANAC160MHZ": return SUPPORTEDTECHNOLOGIES.WLANAC160MHZ;
                case "WLANAX40MHZ": return SUPPORTEDTECHNOLOGIES.WLANAX40MHZ;
                case "WLANAX80MHZ": return SUPPORTEDTECHNOLOGIES.WLANAX80MHZ;
                case "WLANAX160MHZ": return SUPPORTEDTECHNOLOGIES.WLANAX160MHZ;
                case "WLANA20MHZ": return SUPPORTEDTECHNOLOGIES.WLANA20MHZ;
                case "WLANB20MHZ": return SUPPORTEDTECHNOLOGIES.WLANB20MHZ;
                case "WLANG20MHZ": return SUPPORTEDTECHNOLOGIES.WLANG20MHZ;
                case "WLANJ10MHZ": return SUPPORTEDTECHNOLOGIES.WLANJ10MHZ;
                case "WLANJ20MHZ": return SUPPORTEDTECHNOLOGIES.WLANJ20MHZ;
                case "WLANP5MHZ": return SUPPORTEDTECHNOLOGIES.WLANP5MHZ;
                case "WLANP10MHZ": return SUPPORTEDTECHNOLOGIES.WLANP10MHZ;
                case "WLANP20MHZ": return SUPPORTEDTECHNOLOGIES.WLANP20MHZ;
                default:
                    return SUPPORTEDTECHNOLOGIES.CW;
            }
        }
        */

        public void LoadParameter(string strTechnology)
        {
            string strRead;
            string[] splitStr;
            string acprOffsetFreq;
            string WaveformPath;
            string WaveformName;
            string WaveformName2;
            string strCCOffsets;

            #region Waveform Path/Name
            strRead = IniReadValue(strTechnology, "path");
            if (strRead == "")
                WaveformPath = waveformPath;
            else
                WaveformPath = strRead;
            waveformPath = WaveformPath;

            strRead = IniReadValue(strTechnology, "waveform");
            if (strRead.Length < 1)
                WaveformName = "WAVEFORM INVALID or NOT SPECIFIED";
            else
                WaveformName = strRead;
            waveformName = strRead;

            strRead = IniReadValue(strTechnology, "waveform_2");
            if (strRead.Length < 1)
                WaveformName2 = waveformName2;
            else
                WaveformName2 = strRead;
            waveformName2 = WaveformName2;

                #endregion

                #region Format
            Format = IniReadValue(strTechnology, "Format");
            #endregion

            #region Bandwidth
            Bandwidth = IniReadValue(strTechnology, "Bandwidth");
            #endregion

            #region Modulation
            Modulation = IniReadValue(strTechnology, "Modulation");
            #endregion

            #region Direction
            Direction = IniReadValue(strTechnology, "Direction");
            if (Direction.Equals("")) Direction = "UPLINK";
            #endregion

            #region Vramp Mask
            vrampMask = IniReadValue(strTechnology, "VrampMask");
            string VrampPath = waveformPath + "\\";
            vrampMask = VrampPath + vrampMask;
            #endregion

            #region ACPROffsetFreq
            strRead = IniReadValue(strTechnology, "acprOffsetFreq");
            if (strRead.Length < 1)
                acprOffsetFreq = "ACPR Offset Frequency INVALID or NOT SPECIFIED";
            acprOffsetFreq = strRead;
            if (strRead.IndexOf(',') == -1)
            {
                try
                {
                    AcprOffsetFrequencies = new double[1];
                    for (int i = 0; i < AcprOffsetFrequencies.Length; i++)
                        AcprOffsetFrequencies[i] = Convert.ToDouble(strRead);
                }
                catch (Exception ex)
                {
                    acprOffsetFreq = ex.Message;
                }
            }
            else
            {
                try
                {
                    splitStr = strRead.Split(',');
                    AcprOffsetFrequencies = new double[splitStr.Length];
                    for (int i = 0; i < AcprOffsetFrequencies.Length; i++)
                        AcprOffsetFrequencies[i] = Convert.ToDouble(splitStr[i]);
                }
                catch (Exception ex)
                {
                    acprOffsetFreq = ex.Message;
                }

            }
            #endregion

            #region IFBandwidth
            IFBandwidth = IniReadDoubleValue(strTechnology, "ifbandwidth");
            #endregion

            #region Sampling Rate
            SampleRate = IniReadDoubleValue(strTechnology, "sampleRate");
            #endregion

            #region PowerDuration
            PowerDuration = IniReadDoubleValue(strTechnology, "pwrDuration");
            #endregion

            #region ACPRDuration
            AcprDuration = IniReadDoubleValue(strTechnology, "acprDuration");
            #endregion

            #region Trigger Delay in VSA
            strRead = IniReadValue(strTechnology, "filtertype");
            if (strRead == "")
                strRead = "ChannelFilterShapeNone";
            else
                strRead = "ChannelFilterShape" + strRead;
            FilterType = (AnalyzerChannelFilterShapeEnum)Enum.Parse(typeof(AnalyzerChannelFilterShapeEnum), strRead, true);
            #endregion

            #region Filer Alpha
            FilterAlpha = IniReadDoubleValue(strTechnology, "FilterAlpha");
            #endregion

            #region Filter Bandwidth
            FilterBW = IniReadDoubleValue(strTechnology, "FilterBW");
            #endregion

            #region AcprFilterType
            strRead = IniReadValue(strTechnology, "acprfiltertype");
            if (strRead == "")
                strRead = "ChannelFilterShapeNone";
            else
                strRead = "ChannelFilterShape" + strRead;
            AcprFilterType = (AnalyzerChannelFilterShapeEnum)Enum.Parse(typeof(AnalyzerChannelFilterShapeEnum), strRead, true);
            #endregion

            #region ACPR Filter Bandwidth
            AcprFilterBW = IniReadDoubleValue(strTechnology, "acprfilterbw");
            if (AcprFilterBW == -9999)
                AcprFilterBW = 1.28e6;
            #endregion

            #region ACPR Filter Alpha
            AcprFilterAlpha = IniReadDoubleValue(strTechnology, "acprfilteralpha");
            if (AcprFilterAlpha == -9999)
                AcprFilterAlpha = 0;
            #endregion

            #region ACPRSpan
            AcprSpan = IniReadDoubleValue(strTechnology, "acprSpan");
            #endregion

            #region Reference Level Margin
            ReferenceLevelMargin = IniReadDoubleValue(strTechnology, "reflevelmargin");
            #endregion

            #region Digital Level Offset
            DigitalLevelOffset = IniReadDoubleValue(strTechnology, "digleveloffset");
            #endregion

            #region spec harm
            SpecHarmsSpan = IniReadDoubleValue(strTechnology, "specharmsspan");
            SpecHarmsRBW = IniReadDoubleValue(strTechnology, "specharmsrbw");
            SpecHarmsAvgTime = IniReadDoubleValue(strTechnology, "specharmsavgtime");
            #endregion

            #region PVT
            PvtBandwidth = IniReadDoubleValue(strTechnology, "pvtbandwidth");
            PvtSweepTime = IniReadDoubleValue(strTechnology, "pvtsweeptime");
            strRead = IniReadValue(strTechnology, "pvtmarkers");

            string Err;
            if (strRead.Length < 1)
                Err = "PVT Markers INVALID or NOT SPECIFIED";

            if (strRead.IndexOf(',') == -1)
            {
                try
                {
                    PvtMarkers = new double[1];
                    for (int i = 0; i < PvtMarkers.Length; i++)
                        PvtMarkers[i] = Convert.ToDouble(strRead);
                }
                catch (Exception ex)
                {
                    Err = ex.Message;
                }
            }
            else
            {
                try
                {
                    splitStr = strRead.Split(',');
                    PvtMarkers = new double[splitStr.Length];
                    for (int i = 0; i < PvtMarkers.Length; i++)
                        PvtMarkers[i] = Convert.ToDouble(splitStr[i]);
                }
                catch (Exception ex)
                {
                    Err = ex.Message;
                }

            }
            #endregion

            #region EVM Parameter
            EvmSamplingRate = IniReadDoubleValue(strTechnology, "evmSamplingRate");
            EvmSamplingDuration = IniReadDoubleValue(strTechnology, "evmSamplingDuration");
            if (strTechnology.Equals("TDSCDMA"))
            {
                string enable = IniReadValue(strTechnology, "enableHSPA8psk");
                EnableHSPA8psk_TD = (enable.Equals("true")) ? true : false;

            }
            if (strTechnology.ToUpper().Contains("LTE"))
            {
                string boudary = IniReadValue(strTechnology, "evmAnalysisBoundary");
                EvmAnalysisBoundary = (LteAnalysisBoundaryType)Enum.Parse(typeof(LteAnalysisBoundaryType), boudary, true);

                EvmResultLength = IniReadIntValue(strTechnology, "evmResultLength");
            }

            if (strTechnology.ToUpper().Contains("LTEA"))
            {
                CarrierCount = IniReadIntValue(strTechnology, "carrierCount");

                strRead = IniReadValue(strTechnology, "ccOffsets");
                if (strRead.Length < 1)
                    strCCOffsets = "CC Offset Frequency INVALID or NOT SPECIFIED";
                strCCOffsets = strRead;
                if (strRead.IndexOf(',') == -1)
                {
                    try
                    {
                        CCOffsets = new double[1];
                        for (int i = 0; i < CCOffsets.Length; i++)
                            CCOffsets[i] = Convert.ToDouble(strRead);
                    }
                    catch (Exception ex)
                    {
                        strCCOffsets = ex.Message;
                    }
                }
                else
                {
                    try
                    {
                        splitStr = strRead.Split(',');
                        CCOffsets = new double[splitStr.Length];
                        for (int i = 0; i < CCOffsets.Length; i++)
                            CCOffsets[i] = Convert.ToDouble(splitStr[i]);
                    }
                    catch (Exception ex)
                    {
                        strCCOffsets = ex.Message;
                    }

                }
               
            }
            #endregion
        }

    }

    [Serializable]
    public class TestParameters
    {
        public List<TestParameter> testParams { get; set; }

        public TestParameters()
        {
            testParams = new List<TestParameter>();
        }

        public bool LoadINITestParams(string iniFile, string waveformPath)
        {
            bool ret = true;
            try
            {
                List<string> format_list = INIAccess.IniReadAllSection(iniFile);
                foreach (string s in format_list)
                {
                    TestParameter tp = new TestParameter();

                    tp.technology = s;
                    tp.waveformPath = waveformPath;
                    tp.INIFile = iniFile;

                    tp.LoadParameter(s);

                    testParams.Add(tp);
                }
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        TestParameter GetTestParam(int idx)
        {
            if (testParams != null)
                return testParams[idx];
            else
                return null;
        }

        TestParameter GetTestParam(string technology)
        {
            TestParameter tp = null;
            if (testParams == null)
                return null;

            for (int i = 0; i < testParams.Count; i++)
            {
                if (technology.Equals(testParams[i].technology))
                {
                    tp = testParams[i];
                    break;
                }
            }
            return tp;
        }
    }

    public class TestPoint
    {
        public string Unit;
        public double TestResult;
        public double LimitLow;
        public double LimitHigh;
        public double TestTime;

        public double[] ComponentCarrierEvm;

        public TestPoint()
        {
            Unit = "";
            TestResult = -999;
            LimitLow = -999;
            LimitHigh = 999;
            TestTime = -999;
        }
        public TestPoint(string unit, double limitLow, double limitHigh)
        {
            Unit = unit;
            LimitLow = limitLow;
            LimitHigh = limitHigh;
        }
    }

    public sealed class PowerServoInfo
    {
        public double TestFrequency;
        public double DUTInputLoss;
        public double DUTOutputLoss;
        public double DUTTargetPOut;
        public double DUTTargetGain;
        public double PowerServoHeadRoom;
        public double servoOverheadTime = 600e-6;
    }

    public enum numTrace_Enum
    {
        ONE_TRACE = 1,
        TWO_TRACE = 2,
        THREE_TRACE = 3,
        FOUR_TRACE = 4
    }

    public enum measurement_Enum
    {
        S11 = 1,
        S12 = 2,
        S21 = 3,
        S22 = 4
    }

    public enum Format_Enum
    {
        SMITH = 1,
        MLOG = 2,
        PHAS = 3,
        PHASE = 4,
        POLAR = 5
    }

    public enum numSegment_Enum
    {
        ONE_SEGMENT = 1,
        TWO_SEGMENT = 2,
        THREE_SEGMENT = 3,
        FOUR_SEGMENT = 4
    }

    public sealed class SparaConfigure
    {
        public numTrace_Enum num_Trace;

        public measurement_Enum [] measurementType;
        public Format_Enum [] format;
        public double [] HighLimit;
        public double[] LowLimit;

        //public measurement_Enum measurementType2;
        //public Format_Enum format2;
        //public double Limit2;

        //public measurement_Enum measurementType3;
        //public Format_Enum format3;
        //public double Limit3;

        //public measurement_Enum measurementType4;
        //public Format_Enum format4;
        //public double Limit4;

        public numSegment_Enum num_Segment;

        public double [] Start_Freq;
        public double [] Stop_Freq;
        public int [] numPoints;
        public double [] if_bandwidth;
        public double [] power;

        //public double Start_Freq2;
        //public double Stop_Freq2;
        //public int numPoints2;
        //public double if_bandwidth2;
        //public double power2;

        //public double Start_Freq3;
        //public double Stop_Freq3;
        //public int numPoints3;
        //public double if_bandwidth3;
        //public double power3;

        //public double Start_Freq4;
        //public double Stop_Freq4;
        //public int numPoints4;
        //public double if_bandwidth4;
        //public double power4;
    }

    public sealed class TestResultInfo
    {
        public double[] FFTDataOut;
        public double[] IQDataOut;
        public double[] PowerOut;
        #region Gain Compression
        public double GainComp1dB;
        public double GainComp2dB;
        public double GainComp3dB;
        public double GainCompMax;
        public double InPower1dB;
        public double InPower2dB;
        public double InPower3dB;
        public double MaxPOut;
        #endregion
    }

    public sealed class IQDataInfo : ICloneable
    {
        public string tech;
        public double frequency; // Frequency to capture
        public double power;     // power range
        public double sample_rate;     // sample_rate
        public double[] IQ;

        public object Clone()
        {
            IQDataInfo a = new IQDataInfo();
            a.tech = this.tech;
            a.power = this.power;
            a.frequency = this.frequency;
            a.sample_rate = this.sample_rate;
            a.IQ = new double[this.IQ.Length];
            Array.Copy(this.IQ, a.IQ, this.IQ.Length);
            return a;
        }
    }
    public sealed class ArbInfo
    {
        public double arbSampleRate;
        public double arbRmsValue;
        public double arbScale;
        public Source_MarkerEnum rfMarker;
        public Source_MarkerEnum alcMarker;
        public int numSamples;
    }

    public sealed class MinMax
    {
        public static double SupportedSourceMaxOutput;
        public static double SupportedSourceMinFreq;
        public static double SupportedSourceMaxFreq;
        public static double SupportedSourceMinOutputPower;
        public static double SupportedSourceMaxOutputPower;
        public static double SupportedReceiverMaxFreq;
        public static double SupportedReceiverMinFreq;
        public static double SupportedReceiverMaxInputPower;
        public static double SupportedReceiverMinInputPower;
    }

    public sealed class Utility
    {
        //public static double[] inputCalFreqs;
        //public static double[,] inputCalValues;
        //public static double[] outputCalFreqs;
        //public static double[,] outputCalValues;

        public static double[][] CalResFrequency = null;
        public static double[][] CalResInputLoss = null;
        public static double[][] CalResOutputLoss = null;

        public static double[][] CalLossBeforeDUT = null;
        public static double[][] CalLossAfterDUT = null;

        public static string[] NoisefugureCalFileNameForPath = null;

        public static List<double> ENR_table = new List<double>();

        public static bool use_cal_file = false;
        public static double nominal_input_loss = 1.0;
        public static double nominal_output_loss = 1.0;

        public static int pathId;

        public static double getRrcFilterWeight(double freq, double symbolRate, double alpha)
        {
            double m_symbolRate;
            double m_alpha;
            double m_lowerBreakPoint_Hz;
            double m_upperBreakPoint_Hz;
            double m_constant1;         // for Utility.efficiency only
            double m_constant2;         // for Utility.efficiency only

            m_symbolRate = symbolRate;
            m_alpha = alpha;
            m_lowerBreakPoint_Hz = ((1 - alpha) * symbolRate / 2);
            m_upperBreakPoint_Hz = ((1 + alpha) * symbolRate / 2);

            double f = Math.Abs(freq);

            double result;

            if (f <= m_lowerBreakPoint_Hz)
                result = 1;
            else if (f <= m_upperBreakPoint_Hz)
            {
                m_constant1 = (Math.PI / (2 * symbolRate * alpha));
                m_constant2 = (symbolRate * (1 - alpha) / 2);
                result = Math.Cos(m_constant1 * (f - m_constant2));
            }
            else
                result = 0;

            return result;// *result;
        }
        public static double getGausFilterWeight(double freq, double bandwidth)
        {
            double B = 0.5 * bandwidth / (Math.Sqrt(Math.Log(2) / (2 * Math.PI)));
            double f = Math.Abs(freq);
            double result = Math.Exp(-1 * Math.PI * (f / B) * (f / B));

            return result;
        }
        public static double calcPowerFromFftData(double[] fftData, double fOffset, double filtBw, AnalyzerChannelFilterShapeEnum filtType, double filtAlpha,
            double fCenterFreq, double acprSpan, AnalyzerFFTAcquisitionLengthEnum fftSize, double enbw)
        {
            double measPout = 0;
            double fCenter = fCenterFreq;
            double fDelta = acprSpan * 1.25 / (int)fftSize;
            double fStart = fCenter - ((int)fftSize / 2 * fDelta);

            bool isRrcFilter = false;
            bool isGausFilter = false;
            double channelBw = filtBw;
            if (filtType == AnalyzerChannelFilterShapeEnum.ChannelFilterShapeRootRaisedCosine)
            {
                isRrcFilter = true;
                // use a wider BW for RRC filter to get the power in the filter skirt
                channelBw = filtBw * 1.3;
            }
            else if (filtType == AnalyzerChannelFilterShapeEnum.ChannelFilterShapeGaussian)
            {
                isGausFilter = true;
                // use a wider BW for Gaussian filter to get the power in the filter skirt
                channelBw = filtBw * 2.0;
            }
            else
            {
                // for RECT Filter use RC with alpha of 0.03
                filtAlpha = 0.03;
                // use a wider BW for RC filter to get the power in the filter skirt
                channelBw = filtBw * 1.1;
            }

            // calculate start and stop bin for Reference power, including any fractional bin
            double fBinStart = fCenter + fOffset - channelBw / 2;
            int startBin = (int)Math.Floor((fBinStart - fStart) / fDelta);
            int stopBin = (int)Math.Ceiling((fBinStart - fStart + channelBw) / fDelta);
            for (int j = startBin; j <= stopBin; j++)
            {
                double binFreq = (fStart + j * fDelta) - fCenter - fOffset;
                double binScale;
                if (isRrcFilter)
                {
                    binScale = getRrcFilterWeight(binFreq, filtBw, filtAlpha);
                }
                else if (isGausFilter)
                {
                    binScale = getGausFilterWeight(binFreq, filtBw);
                }
                else
                {
                    binScale = getRrcFilterWeight(binFreq, filtBw, filtAlpha);
                    binScale *= binScale; // Square result from RRC filter weight
                }
                measPout += fftData[j] * binScale;
            }
            measPout = 10 * Math.Log10(measPout);
            // Correct for the ENBW of FFT Window choice
            measPout -= 10 * Math.Log10(enbw);
            return measPout;
        }
        public static double voltsSquare2dBm(double voltsSquared, double impedance)
        {
            return 10 * System.Math.Log10(voltsSquared / (.001 * impedance));
        }
        public static double linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }
        public static void CopyFillComplexArray(float[] src, float[] dst)
        {
            // Copy src array to dst
            Array.Copy(src, dst, Math.Min(src.Length, dst.Length));

            int count = (dst.Length - src.Length) / 2;  // complex pairs
            if (count <= 0)
                return;

            // Zero-hold last complex value to fill the destination array
            int index = src.Length;
            float r = src[index - 2];
            float i = src[index - 1];
            while (--count >= 0)
            {
                dst[index++] = r;
                dst[index++] = i;
            }
        }
        public static float[] convertToFloat(double[] inputArray)
        {
            if (inputArray == null)
                return null;

            float[] output = new float[inputArray.Length];
            for (int i = 0; i < inputArray.Length; i++)
                output[i] = (float)inputArray[i];

            return output;
        }
        public static bool AboutEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }
        public static void Delay(int mSec)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < mSec)
            {
            }
        }
        public static void Write89600file(string filePath, string fileName, double[] dataArray, double centerFreq, double sampleRate)
        {
            string logFileName = Path.Combine(filePath, fileName);
            StreamWriter arrayFile = new StreamWriter(logFileName, false);

            arrayFile.WriteLine("InputCenter\t" + centerFreq.ToString());
            arrayFile.WriteLine("InputZoom\tTRUE");
            arrayFile.WriteLine("InputRefImped\t50.0");
            arrayFile.WriteLine("XStart\t0.0");
            arrayFile.WriteLine("XDelta\t" + (1 / sampleRate).ToString());
            arrayFile.WriteLine("YScale\t1.0");

            for (int i = 0; i < dataArray.Length; i = i + 2)
            {
                arrayFile.WriteLine(dataArray[i].ToString("0.000000") + "\t" + dataArray[i + 1].ToString("0.000000"));
            }
            arrayFile.Close();
        }
        public static void setPathId(int id)
        {
            //path Id start with 0
            if (id >= 1)
                pathId = id - 1;

        }
        public static double getOutputAtten(double frequency)
        {
            if (use_cal_file)
            {
                if (frequency <= CalResFrequency[pathId][0])
                    return CalResOutputLoss[pathId][0];
                if (frequency >= CalResFrequency[pathId][CalResFrequency[pathId].Length - 1])
                    return CalResOutputLoss[pathId][CalResFrequency[pathId].Length - 1];
                int index = 0;
                while (frequency > CalResFrequency[pathId][index]) index++;
                return linear(frequency, CalResFrequency[pathId][index - 1], CalResFrequency[pathId][index], CalResOutputLoss[pathId][index - 1], CalResOutputLoss[pathId][index]);
            }
            else
            {
                return nominal_output_loss;
            }
        }
        public static double getInputAtten(double frequency)
        {
            if (use_cal_file)
            {
                if (frequency <= CalResFrequency[pathId][0])
                    return CalResInputLoss[pathId][0];
                if (frequency >= CalResFrequency[pathId][CalResFrequency[pathId].Length - 1])
                    return CalResInputLoss[pathId][CalResFrequency[pathId].Length - 1];
                int index = 0;
                while (frequency > CalResFrequency[pathId][index]) index++;
                return linear(frequency, CalResFrequency[pathId][index - 1], CalResFrequency[pathId][index], CalResInputLoss[pathId][index - 1], CalResInputLoss[pathId][index]);
            }
            else
            {
                return nominal_input_loss;
            }
        }
        public static double[] getLossBeforeDUTTable()
        {
            return CalLossBeforeDUT[pathId];
        }

        public static double[] getLossAfterDUTTable()
        {
            return CalLossAfterDUT[pathId];
        }

        public static double[] getCalResFrequency()
        {
            return CalResFrequency[pathId];
        }

        public static void readCalData(string CalResFileName, ref double[][] CalResFrequency, ref double[][] CalResInputLoss, ref double[][] CalResOutputLoss,
                                                              ref double[][] CalLossBeforeDUT, ref double[][] CalLossAfterDUT)
        {
            var rs = new FileStream(CalResFileName, FileMode.Open);
            TextReader calFileReader = new StreamReader(rs);
            string[] columns = null;
            string calFileReadString = null;
            int iNumCalPath = 0;
            string sTempString = null;

            for (int i = 0; i < 6; i++)
            {
                calFileReadString = calFileReader.ReadLine();
            }

            if (calFileReadString != null)
            {
                calFileReadString = calFileReadString.ToUpper();
                columns = calFileReadString.Split(',');
                if (columns[0].Contains("PATH NUMBER"))
                    iNumCalPath = Convert.ToInt32(columns[1]);
            }
            else
            {
                return;
            }

            string[] readOutCalibratedResult = new string[iNumCalPath * 3];
            int j = 0;
            while ((calFileReadString = calFileReader.ReadLine()) != null)
            {
                columns = calFileReadString.Split(',');
                if (columns[0].Contains("Path"))
                {
                    readOutCalibratedResult[3 * j] = calFileReadString;
                    readOutCalibratedResult[3 * j + 1] = calFileReader.ReadLine();
                    readOutCalibratedResult[3 * j + 2] = calFileReader.ReadLine();
                    j++;
                }
                else
                {
                    calFileReader.ReadLine();
                    calFileReader.ReadLine();
                }
            }

            int iPathId = 0;
            int iNumberPoint = 0;

            for (int k = 0; k < iNumCalPath; k++)
            {
                columns = readOutCalibratedResult[3 * k].Split(',');
                sTempString = (columns[0]).Replace("Path", "");
                iPathId = Convert.ToInt32(sTempString) - 1;
                iNumberPoint = Convert.ToInt32(columns[2]);

                //since KMF 1.16.20.0, the calibration table should be in format, loss1,loss2,loss3...removed frequency information
                CalLossBeforeDUT[iPathId] = new double[iNumberPoint];
                CalLossAfterDUT[iPathId] = new double[iNumberPoint];

                CalResFrequency[iPathId] = new double[iNumberPoint];
                for (int m = 0; m < iNumberPoint; m++)
                {
                    CalResFrequency[iPathId][m] = Convert.ToInt64(columns[4 + m]);
                }

                columns = readOutCalibratedResult[3 * k + 1].Split(',');
                CalResInputLoss[iPathId] = new double[iNumberPoint];
                for (int m = 0; m < iNumberPoint; m++)
                {
                    CalResInputLoss[iPathId][m] = Convert.ToDouble(columns[4 + m]);
                    CalLossBeforeDUT[iPathId][m] = CalResInputLoss[iPathId][m];
                }

                columns = readOutCalibratedResult[3 * k + 2].Split(',');
                CalResOutputLoss[iPathId] = new double[iNumberPoint];
                for (int m = 0; m < iNumberPoint; m++)
                {
                    CalResOutputLoss[iPathId][m] = Convert.ToDouble(columns[4 + m]);
                    CalLossAfterDUT[iPathId][m] = CalResOutputLoss[iPathId][m];
                }

            }

            calFileReader.Close();
            rs.Close();

        }


        public static void readNoiseFigureCalDataFilesMapping(string NSCalDataMappingFileName, ref string[] CalDataAndPathMapping)
        {
            int iMaximumPathNumber = 20;
            string CalFileNameForThisPath = null;

            FileInfo fileInfo = new FileInfo(NSCalDataMappingFileName);
            string sDirectoryName = fileInfo.DirectoryName;

            bool readXmlFile = true;

            if (fileInfo.Extension == ".ini")
            {
                readXmlFile = false;
            }
            else
            {
                readXmlFile = true;
            }


            string sSectionName = null;
            string sKeyName = null;

            if (!readXmlFile)
            {
                INIAccess iniAccess = new INIAccess(NSCalDataMappingFileName);

                for (int i = 0; i < iMaximumPathNumber; i++)
                {
                    sSectionName = "Path_" + (i + 1).ToString();
                    sKeyName = "NSCalFileName";
                    CalFileNameForThisPath = iniAccess.IniReadValue(sSectionName, sKeyName);
                    CalDataAndPathMapping[i] = sDirectoryName + "\\" + CalFileNameForThisPath;
                }
            }

            XmlDocument xd = new XmlDocument();
            xd.Load(NSCalDataMappingFileName);

            foreach (XmlNode x in xd.DocumentElement)
            {
                #region Instrument Port
                if (x.Name.Equals("CalFile"))
                {
                    int pathId = Convert.ToInt32((x.Attributes["Path"].Value).Replace("Path", ""))-1;
                    string FileDirectory = x.Attributes["File"].Value;

                    CalDataAndPathMapping[pathId] = FileDirectory;
                }
                #endregion
            }

        }

        public static List<double> getENRTable()
        {
            return ENR_table;
        }

        public static void readNoiseFigureENRTable(string NS_ENR_table_file)
        {
            //Read ENR data
            if (ENR_table != null)
            {
                ENR_table.Clear();
            }

            // Open the file
            var rs = new FileStream(NS_ENR_table_file, FileMode.Open);
            System.IO.StreamReader rd = new StreamReader(rs);

            string _str;
            string[] _str2;
            string[] columns;

            List<string[]> _list = new List<string[]>();
            _list.Clear();

            while (rd.EndOfStream == false)
            {
                _str = rd.ReadLine().TrimStart('\t');
                //if (_str.StartsWith("!") || (_str.Length == 0) || _str.StartsWith("["))
                //{
                //    //do nothing
                //}
                //else
                //{
                //    _str2 = _str.Split('\t', ' ').Select(s => s.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                //    if (_str2[1].IndexOf("MHz") != -1)
                //    {
                //        //_str2[0] = string.Concat(System.Text.RegularExpressions.Regex.Match(_str2[0], "^[0-9]+").ToString(), "e6"); // convert unit from MHz to Hz
                //        _str2[0] = string.Concat(_str2[0], "e6"); // convert unit from MHz to Hz
                //    }
                //    _list.Add(_str2);
                //}
                if (_str.Contains("Frequency")|| _str.Contains("Version") || _str.Contains("Serialnumber") || _str.Contains("Model"))
                {
                    //do nothing
                }
                else
                {
                    columns = _str.Split(',');
                    _list.Add(columns);
                }
            }

            rd.Close();
            rs.Close();

            // set ENR data table that is {freq1(Hz), ENR1(dB), freq2(Hz), ENR2(dB), ...) format
            ENR_table.Clear();
            for (int i = 0; i < _list.Count; i++)
            {
                _str2 = _list[i];
                if (double.Parse(_str2[0]) <= 60e9)
                {
                    ENR_table.Add(double.Parse(_str2[0]));
                    ENR_table.Add(double.Parse(_str2[1]));
                }
            }
        }


        public static string getNSCalFileNameForPath()
        {
            string CalFileName = null;
            CalFileName = NoisefugureCalFileNameForPath[pathId];
            return CalFileName;
        }

        public static int GetSiteId()
        {
            int siteid = 1;
            char sSiteid = '1';
            try
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory;
                string siteFile = Path.Combine(path, "SiteInstrument.xml");
                //find start index of "Site"
                int index = path.IndexOf("Site");

                if (index != -1)
                {
                    //Get site id string ,such as 1, 2,3,4.
                    sSiteid = (path.ToCharArray())[index + 4];
                }
                switch (sSiteid)
                {
                    case '1':
                        siteid = 1;
                        break;
                    case '2':
                        siteid = 2;
                        break;
                    case '3':
                        siteid = 3;
                        break;
                    case '4':
                        siteid = 4;
                        break;
                    default:
                        break;

                }
            }
            catch (Exception ex)
            {
            }
            return siteid;
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }


    }

    public class PowerSolutionExt
    {
        #region Fields
        private double rbw;
        private double nbw;
        private double span;
        private double[] points;
        private double interval;
        private long numPoints;

        public double Rbw
        {
            get { return rbw; }
            set { rbw = value; }
        }
        public double Nbw
        {
            get { return nbw; }
            set { nbw = value; }
        }
        public double Span
        {
            get { return span; }
            set { span = value; }
        }
        public double[] Points
        {
            get { return points; }
            set { points = value; }
        }
        public double Interval
        {
            get { return interval; }
            set { interval = value; }
        }
        public long NumPoints
        {
            get { return numPoints; }
            set { numPoints = value; }
        }
        #endregion

        #region Contructors
        // Look at constructor below to figure out parameters.
        public PowerSolutionExt(
            double rbw,
            double nbw,
            double span,
            long numPoints,
            double[] points,
            bool dbmOrIQPairs
        )
        {
            this.rbw = rbw;
            this.nbw = nbw;
            this.span = span;
            // Convert pairs to dbm if in pairs, else just leave alone.
            if (dbmOrIQPairs)
            {
                this.points = points;
            }
            else
            {
                this.points = new double[points.Length >> 1];
                for (int i = 0; i < this.points.Length; i++)
                {
                    int pi = i << 1;
                    this.points[i] = 10 * Math.Log(points[pi] * points[pi] + points[pi + 1] * points[pi + 1]);
                }
            }
            this.interval = 0;
            this.numPoints = numPoints;
        }

        /// <summary>
        /// Constructor with all possible parameters.
        /// </summary>
        /// <param name="rbw"> Target resolution bandwidth; RBW of the equation. </param>
        /// <param name="nbw"> Noise band width, according to Bob generally a constant around 1.05 </param>
        /// <param name="span"> The frequency length of the signal being measured (frequency length of points). 
        ///                     The span MUST BE >= 1.0 </param>
        /// <param name="points"> The data points being integrated over. </param>
        /// <param name="dbmOrIQPairs"> True means points are coming in in dbm, false means IQ pairs. </param>
        /// <param name="interval"> The frequency length of a single bucket. </param>
        public PowerSolutionExt(
            double rbw,
            double nbw,
            double span,
            double[] points,
            bool dbmOrIQPairs,
            double interval,
            long numPoints
        )
        {
            this.rbw = rbw;
            this.nbw = nbw;
            this.span = span;
            // Convert pairs to dbm if in pairs, else just leave alone.
            if (dbmOrIQPairs)
            {
                this.points = points;
            }
            else
            {
                this.points = new double[points.Length >> 1];
                for (int i = 0; i < this.points.Length; i++)
                {
                    int pi = i << 1;
                    this.points[i] = 10 * Math.Log(points[pi] * points[pi] + points[pi + 1] * points[pi + 1]);
                }
            }
            this.interval = interval;
            this.numPoints = numPoints;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Calculates the integrated power for the entire span.
        /// </summary>
        public double getWholeSpanIntegratedPower()
        {
            return getSpanIntegratedPower(0, points.Length);
        }

        /// <summary>
        /// Gets the integrated power of an interval. Here, start and end refer to the indices in 
        /// the points array to integrate over.
        /// </summary>
        /// <param name="start"> Inclusive, starts from index start. </param>
        /// <param name="end"> Not inclusive, will not include the end-th point </param>
        /// <returns></returns>
        private double getSpanIntegratedPower(
            long start,
            long end
            )
        {
            double power = 10 * Math.Log10(span / (rbw * nbw * (numPoints - 1)));
            double summation = 0.0;
            for (long i = start; i < end; i++)
            {
                summation += Math.Pow(10, points[i] / 10);
            }
            return power + 10 * Math.Log10(summation);
        }
        #endregion
    }

    public class AwgPulseConfig
    {
        public string vsgArbName { get; set; }
        public string awgArbName { get; set; }
        public string awgArbDisplayName { get; set; }
        public double arbLength { get; set; }
        public double trigDelay { get; set; }
        public double leadTime { get; set; }
        public double lagTime { get; set; }
        public double pulseVoltage { get; set; }
        public double dutyCycle { get; set; }

        public AwgPulseConfig() { }
    }

    /*
    public class PAMBHelp
    {
        public static bool IsTdscdma(SUPPORTEDTECHNOLOGIES tech)
        {
            if (tech == SUPPORTEDTECHNOLOGIES.TDSCDMA)
                return true;
            else
                return false;
        }

        public static bool IsLTE(SUPPORTEDTECHNOLOGIES tech)
        {
            switch (tech)
            {
                case SUPPORTEDTECHNOLOGIES.LTE5MHZ:
                case SUPPORTEDTECHNOLOGIES.LTE10MHZ:
                case SUPPORTEDTECHNOLOGIES.LTE20MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD10MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD5MHZ:
                case SUPPORTEDTECHNOLOGIES.LTETDD20MHZ:
                case SUPPORTEDTECHNOLOGIES.LTE20_20MHZ:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWLAN(SUPPORTEDTECHNOLOGIES tech)
        {
            switch (tech)
            {
                case SUPPORTEDTECHNOLOGIES.WLANAC160MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAC40MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAC80MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAC20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAX160MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAX40MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAX80MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANAX20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANN20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANN40MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANA20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANB20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANG20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANJ10MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANP5MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANP10MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANJ20MHZ:
                case SUPPORTEDTECHNOLOGIES.WLANP20MHZ:

                    return true;
                default:
                    return false;
            }
        }
    }
    */
    public class InstrDetailInfo
    {
        public string Identifier { set; get; }
        public string Revision { set; get; }
        public string Vendor { set; get; }
        public string Model { set; get; }
        public string FirmwareRev { set; get; }
        public string RecommendedRev { set; get; }

        public InstrDetailInfo()
        {
            Identifier = string.Empty;
            Revision = string.Empty;
            Vendor = string.Empty;
            Model = string.Empty;
            FirmwareRev = string.Empty;
            RecommendedRev = string.Empty;
        }
        public void Clear()
        {
            Identifier = string.Empty;
            Revision = string.Empty;
            Vendor = string.Empty;
            Model = string.Empty;
            FirmwareRev = string.Empty;
            RecommendedRev = string.Empty;
        }

    }


    #endregion

    #region xApp Measurement
    // mui for display and rui for scpi purpose hxiao, 2016.12.9
    public enum ORFSTYPE
    {
        Modulation,
        ModulationAndSwitching,
        Switching,
        FullFrame
    };
    public enum ruiDIRECTION
    {
        MS,
        BTS
    };
    public enum muiTriggerSource
    {
        External1 = 1,
        External2 = 2,
        Immediate = 3,
        //Line = 4,
        //Frame = 5,
        RFBurst = 6,
        //Video = 7,
        PXI = 20
    }
    public enum ruiTriggerSource
    {
        EXTernal1 = 1,
        EXTernal2 = 2,
        IMM = 3,
        //LINE = 4,
        //FRAMe = 5,
        RFBurst = 6,
        //VIDeo = 7,
        PXI = 20
    }
    public enum xOnOffMode
    {
        OFF = 0,
        ON = 1
    }

    public enum xAutoMode
    {
        Manual = 0,
        Auto = 1
    }
 
    public enum muiOrfsMethod
    {
        MultiOffset = 0,
        SingleOffset = 1,
        Swept = 2
    }
    public enum ruiOrfsMethod
    {
        MULTiple = 0,
        SINGle = 1,
        SWEPt = 2
    }
    public enum muiOrfsFreqList
    {
        StandardList = 0,
        ShortList = 1,
        //CustomList = 2,
        LimitList
    }
    public enum ruiOrfsFreqList
    {
        STANdard = 0,
        SHORt = 1,
        //CUSTom = 2,
        LCUStom
    }

    //     TD-SCDMA timeslots within a sub-frame.
    public enum TDTimeSlotType
    {

        TS0 = 0,      
        DwPTS = 1,    
        UpPTS = 2,
        TS1 = 3,      
        TS2 = 4,   
        TS3 = 5,   
        TS4 = 6,  
        TS5 = 7,
        TS6 = 8
    };

    public enum LteAnalysisBoundaryType
    {
        Frame,
        HalfFrame,
        Subframe,
        Slot
    };

    public class xAppDomainBase: MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
    }
    public class xCommonSetting : xAppDomainBase
    {
        public xOnOffMode avgstate;
        public Int16 avgnumber;
        public muiTriggerSource trigsource;
        public double triglevel;
        public PXIChassisTriggerEnum pxi_trigger;
        public double trigger_delay;

        public List<string> scpi_list;

        public xCommonSetting(xOnOffMode state, Int16 num, 
                                 muiTriggerSource source, 
                                 double level,
                                 PXIChassisTriggerEnum pxi,
                                 double delay,
                                 List<string> scpis)
        {
            avgstate = state;
            avgnumber = num;
            trigsource = source;
            triglevel = level;
            pxi_trigger = pxi;
            trigger_delay = delay;
            scpi_list = scpis;
        }
    };

    public class xTDSDMASetting : xAppDomainBase
    {
        public TDTimeSlotType timeslot;
        public bool enableHspa8Psk;

        public xTDSDMASetting(TDTimeSlotType slot, bool enable)
        {
            timeslot = slot;
            enableHspa8Psk = enable;
        }
    };

    public class xEvmLTESetting : xAppDomainBase
    {
        public int carriernum;
        public double[] cc_offset = new double[5];


        //public xEvmLTESetting(int num, string Offsets)
        //{
        //    string[] offset;
        //    int len;
        //    carriernum = num;

        //    if (Offsets != String.Empty)
        //    {
        //        offset = Offsets.Split(';');
        //        len = offset.GetLength(0);

        //        for (int i = 0; i < len; i++)
        //        {
        //            cc_offset[i] = Convert.ToDouble(offset[i]);
        //        }
        //    }

        //}
    }

    public class xOrfsSetting : xAppDomainBase
    {
        public ORFSTYPE meastype;
        public muiOrfsMethod measmethod;
        public muiOrfsFreqList freqlist;
    }
    public class xEvmResult : xAppDomainBase
    {
        public bool result; //passfail
        public carrierEvmResult[] carEvmResults;
        public xEvmResult(int num)
        {
            int i;
            carEvmResults = new carrierEvmResult[num];
            for (i = 0; i < num; i++)
            {
                carEvmResults[i] = new carrierEvmResult(-9.990000000E+2);
            }
        }
    }
    public class carrierEvmResult : xAppDomainBase
    {
        public double rms_evm;
        public double max_rms_evm;
        public double peak_evm;
        public double peak_evm_index;
        public double max_peak_evm;
        public double max_peak_evm_index;
        public double mag_err;
        public double max_mag_err;
        public double phase_err;
        public double max_phase_err;
        public double peak_phase_err;
        public double max_peak_phase_err;
        public double oof;
        public double max_oof;
        public double freq_err;
        public double max_freq_err;
        public double rho;
        public double peak_cde;
        public double pcde_ch_num;
        public double active_ch_num;
        public double time_offset;
        public double trig_to_T0;
        public double power;
        public double data_evm;
        public double qpsk_evm;
        public double qam16_evm;
        public double qam64_evm;
        public double rs_evm;
        public double sync_corr;
        public double track_err;
        public double symb_clk_err;
        public double max_symb_clk_err;
        public double iq_gain_inbaln;
        public double max_iq_gain_inbaln;
        public double iq_quad_err;
        public double max_iq_quad_err;
        public double iq_time_skew;
        public double max_iq_time_skew;
        public double cell_id;
        public double ref_rx_pwr;
        public double ref_rx_qua;
        public double rx_si;

        // LTE specific
        public double inband_emi;
        public double inband_margin;
        public double inband_ws_slot;
        public double inband_ws_carrier;
        public double inband_ws_rb;
        public double spec_flat;
        public double spec_flat_margin;
        public double spec_flat_slot;
        public double spec_flat_subcar;
        public double ref_tx_pwr;
        public double symb_tx_pwr;
        public double rssi;

        // WLAN specific
        public double max_burst_avgpwr;
        public double burst_avgpwr;
        public double max_burst_peakpwr;
        public double burst_peakpwr;
        public double max_burst_p2apwr;
        public double burst_p2apwr;
        public double data_mod_format;
        public double max_pilot_rmsevm;
        public double pilot_rmsevm;
        public double max_data_pkevm;
        public double data_pkevm;
        public double burst_num;

        public carrierEvmResult(double val)
        {
            rms_evm = val;
            peak_evm = val;
            peak_evm_index = 0;
            mag_err = val;
            phase_err = val;
            oof = val;
            max_oof = val;
            freq_err = val;
            max_rms_evm = val;
            max_peak_evm = val;
            max_peak_evm_index = 0;
            max_mag_err = val;
            max_phase_err = val;
            max_freq_err = val;
            peak_phase_err = val;
            max_peak_phase_err = val;
            rho = val;
            peak_cde = val;
            pcde_ch_num = 0;
            active_ch_num = 0;
            time_offset = val;
            trig_to_T0 = val;
            power = val;
            data_evm = val;
            qpsk_evm = val;
            qam16_evm = val;
            qam64_evm = val;
            rs_evm = val;
            sync_corr = val;
            track_err = val;
            symb_clk_err = val;
            max_symb_clk_err = val;
            iq_gain_inbaln = val;
            max_iq_gain_inbaln = val;
            iq_quad_err = val;
            max_iq_quad_err = val;
            iq_time_skew = val;
            cell_id = 0;
            ref_rx_pwr = val;
            ref_rx_qua = val;
            rx_si = val;
            inband_emi = 0;
            inband_margin = val;
            inband_ws_slot = 0;
            inband_ws_carrier = 0;
            inband_ws_rb = 0;
            spec_flat = 0;
            spec_flat_margin = val;
            spec_flat_slot = 0;
            ref_tx_pwr = val;
            symb_tx_pwr = val;
            rssi = val;
            max_burst_avgpwr = val;
            burst_avgpwr = val;
            max_burst_peakpwr = val;
            burst_peakpwr = val;
            max_burst_p2apwr = val;
            burst_p2apwr = val;
            data_mod_format = 0;
            max_pilot_rmsevm = val;
            pilot_rmsevm = val;
            max_data_pkevm = val;
            data_pkevm = val;
            burst_num = 0;
        }
    }
    public class xSemSegment : xAppDomainBase
    {
        public double neg_relpower;
        public double neg_abspower;
        public double neg_relpeakpower;
        public double neg_abspeakpower;
        public double neg_peakfreq;
        public double pos_relpower;
        public double pos_abspower;
        public double pos_relpeakpower;
        public double pos_abspeakpower;
        public double pos_peakfreq;
        public xSemSegment(double val)
        {
            neg_relpower = val;
            neg_abspower = val;
            neg_relpeakpower = val;
            neg_abspeakpower = val;
            neg_peakfreq = val;

            pos_relpower = val;
            pos_abspower = val;
            pos_relpeakpower = val;
            pos_abspeakpower = val;
            pos_peakfreq = val;
        }
    }
    public class xSemResult : xAppDomainBase
    {
        public bool result; //passfail
        public double cf_power;
        public double left_power;
        public double right_power;
        public double cf_peakfreq;
        public double left_peakfreq;
        public double right_peakfreq;
        public xSemSegment A;
        public xSemSegment B;
        public xSemSegment C;
        public xSemSegment D;
        public xSemSegment E;
        public xSemSegment F;
        public xSemSegment G;
        public xSemSegment H;
        public xSemSegment I;
        public xSemSegment J;
        public xSemSegment K;
        public xSemSegment L;

        public double negA_margin;
        public double posA_margin;
        public double negB_margin;
        public double posB_margin;
        public double negC_margin;
        public double posC_margin;
        public double negD_margin;
        public double posD_margin;
        public double negE_margin;
        public double posE_margin;
        public double negF_margin;
        public double posF_margin;
        public double negG_margin;
        public double posG_margin;
        public double negH_margin;
        public double posH_margin;
        public double negI_margin;
        public double posI_margin;
        public double negJ_margin;
        public double posJ_margin;
        public double negK_margin;
        public double posK_margin;
        public double negL_margin;
        public double posL_margin;
        public xSemResult(double val)
        {
            A = new xSemSegment(val);
            B = new xSemSegment(val);
            C = new xSemSegment(val);
            D = new xSemSegment(val);
            E = new xSemSegment(val);
            F = new xSemSegment(val);
        }
    }

    public class xSemResultPassFail : xAppDomainBase
    {
        public bool pass; //passfail
        public bool neg_a_pass;
        public bool pos_a_pass;

        public bool neg_b_pass;
        public bool pos_b_pass;

        public bool neg_c_pass;
        public bool pos_c_pass;

        public bool neg_d_pass;
        public bool pos_d_pass;

        public bool neg_e_pass;
        public bool pos_e_pass;

        public bool neg_f_pass;
        public bool pos_f_pass;

        public bool neg_g_pass;
        public bool pos_g_pass;

        public bool neg_h_pass;
        public bool pos_h_pass;

        public bool neg_i_pass;
        public bool pos_i_pass;

        public bool neg_j_pass;
        public bool pos_j_pass;

        public bool neg_k_pass;
        public bool pos_k_pass;

        public bool neg_l_pass;
        public bool pos_l_pass;

        public xSemResultPassFail()
        {

        }
    }
    public class OrfsxAppResult : xAppDomainBase
    {
        public double[] offset_result;
        public double mod_rel_power;
        public double mod_abs_power;
        public double switch_rel_power;
        public double switch_abs_power;

        public double swept_frequency;
        public double swept_freq_offset;
        public double swept_abs_pwr;
        public double swept_delta_2limit;
        public double swept_delta_2ref;

    }
    // will be align with KMF finally, hxiao
    public class xOrfsResult : xAppDomainBase // finally should be align with KMF? hxaio
    {
        public bool bPassed;//passfail
        public ORFSTYPE meas_type;
        public muiOrfsMethod method;

        public OrfsxAppResult meas_result;
    }
    #endregion

    #region KMF
    public enum GSMBand
    {
        GSM,
        DCS1800,
        PCS1900
    }
    public enum KMFORFSType
    {
        Modulation,
        ModulationAndSwitching,
        Switching
    };

    public enum KMFORFSBurst
    {
        Normal,
        Hsr,
        Mixed
    };

    public enum KMFORFSOffsetList
    {
        Standard,
        Short,
        Custom
    };



    public enum KMFORFSPCL
    {
        Auto,
        Manual
    };

    public enum AverageType
    {
        Log,
        Rms
    };

    public enum KMFLimitConfig
    {
        Preset,
        Custom
    };
    #endregion

    #region Interface Definition

    public interface TestPlugIn
    {
        bool PerformSetup { get; set; }
        bool SetParameter(object parameterClass);
        bool StartTest(object parameterToDeliver);
    }

    public interface IPXIChassisM901XX : IInstrument
    {
        bool ConfigTriggerBus(int trig, PXI_CHASSIS_TRIGGER_BUS_CONFIG config);
    }

    public interface IInstrument
    {
        bool Open(string Resource, string Options);
        bool Close();
        Version firmwareRevision { get; set; }
        bool Initialized();
    }

    public interface IScpi: IInstrument
    {
        bool SendCommand(string cmd);
        bool SendQuery(string cmd);
        string Address { get; set; }
    }

    public interface IReference : IInstrument
    { }
    
    public interface IXApp
    {
        void SendCommandToXApp(string cmd);
        string ReadFromXApp();
        void SetXAppMode(bool isLocal);
        void Lock(bool abortXApp);
        void Unlock();
    }

    public interface IDCSMU : IInstrument
    {
        // Properties
        string dcSmuAddress { set; get; }
        DCSMUInfo[] dcSmuInfo { get; set; }

        // Methods
        int GetNumChans();
        bool ConfigSmuLogicalChanMapping(int physChan, int logChan);
        bool SetupSmus(double measTime = 0.001);
        bool OpenSmuOutputRelays();
        bool CloseSmuOutputRelays();
        bool MeasCurrent(DCSMUInfo.SMU_CHANTYPE chanType, ref double current, double range = 3.0);
        bool MeasCurrent(int LogicChannel, ref double current, double range = 3.0);
        bool MeasDCPower(ref double current);
    }

    public interface INoiseSource : IInstrument
    {
 
    }

    public interface IPowerSensor : IInstrument
    {
        bool ZeroPowerSensor();
        bool ContinuousOff();
        bool Initiate();
        bool Fetch(ref string result);
        bool ConfigExpectedPower(double ExpectedPower);
        bool ConfigFrequency(double frequency);
    }

    public interface IVNA : IInstrument
    {
        //Properties

        //Methods
        bool setVnaVisible(bool isVisible);

        //bool setVnaLocal(bool isLocal);
        bool setActiveChannelLocal(int chanNum);
        
        bool readVnaSetupData(string fileName);

        bool loadVnaStateFile(string fileName);

        bool setupStateFileData();

        //bool passVnaSetupData(SparaConfigure sparaConfig);

        //bool calVna();

        bool configSparms();

        bool measSparms(int chanNum, ref bool[] result);

        bool saveTracesToCSV(int chanNum);

        bool saveTracesToSnP(int chanNum, string SnP, string PortNumbers);

        bool getNumTraceOfChannel(int chanNum, ref int numTrace, ref bool pass);

        bool getNumChannels(ref int numChannel);

        bool getPathNumOfChannel(int chanNum, ref string pathNum);
    }

    public interface IDIO : IInstrument
    {
        string rffeResource { set; get; }
        DIOInfo[] info { get; set; }

        bool LoadSTILConfig(string Mode);

        bool GetCommandLoadStatus();
        bool sendRffeCommands();
        bool readRffeCommands(string fileName);
        bool setVioState(bool state);
        bool FEMStateTrans(FEM_STATE_TRANSITION eFEM_State_Transition);

        bool PatternSiteWithHiZ();
        bool writeRffe(int addr, int register, int data, string command);
        bool readRffe(int addr, int register, string command, ref int value);

        bool I2CGetCommandLoadStatus();
        bool I2CsendCommands();
        bool I2CreadCommands(string fileName);
        bool I2Cwrite(int addr, int register, int data);
        bool I2Cread(int addr, int register, ref int value);

    }

    public interface IPPMU
    {
        void ppmuSetup(bool activate = true);
        void ppmuHiZ(string chan);
        void ppmuFc(string channel, double current);
        void ppmuFv(string channel, double voltage);
        double[] ppmuFvmc(string channel, double voltage, double current_limit);
        double[] ppmuFvmv(string channel, double voltage);
        double[] ppmuFcmv(string channel, double current);
        double[] ppmuFcmc(string channel, double current);
        double[] ppmuFnmv(string channel);
    }

    public interface IAWG : IInstrument
    {
        string arbResource { set; get; }
        double arbTrigDelay { set; get; }
        double maxSampleRate { get; set; }
        double minSampleRate { get; set; }
        double spurFreq { set; get; }
        double spurAmp { set; get; }
        int AWGChannel { set; get; }
        int PartnerChannel { set; get; }
        double channelOffset { set; get; }
        double IdealSampleDelay { set; get; }    
        int MaxDacScale { set; get; }
        double ClockFrequency { set; get; }
        object AWG_ { get; set; }

        bool ClearMemory();
        //bool LoadWaveform(string filePath, string fileName, string refName, double vRef, double etpsGain, bool useShortWaveform = false, double shortTime = 0);
        bool LoadWaveform(float[] waveformData, double etSampleRate, string refName, double vRef, double etpsGain, bool useShortWaveform = false, double shortTime = 0);
        bool LoadWaveform(short[] waveformData, double etSampleRate, string refName);       
        bool PlayWaveform(string fileName, bool ext_trigger = true);
        bool PlayWaveformForFlexSampleRate(string fileName, bool ext_trigger = true, int nAWGDelay = 0);
        void Stop();
        bool playPulseMode(int triggernum, double pulseWidth, double leadTime, double lagTime, double dutyCycle, double pulseVoltage, double triggerDelay, bool isVXT);
        bool playVrampMask(int triggernum, ref double[] vrampMask, double dVrampVoltage);
        bool SetChannelAmplitude(int channelNumber, double Amplitude);
        int SetupMarker(int nAWG, int markerMode, int trgPXImask, int trgIOmask, int value, int syncMode, int length5Tclk, int delay5Tclk);
        void SetTriggerOut(int triggernum, int mode);
        void SetTriggerIn(int triggernum, int mode);

        bool genAndCachePulse(string refName, double pulseWidth, double leadTime, double lagTime, double dutyCycle, double pulseVoltage, double dRFTriggerDelay, bool isVXT);
        bool playCachedPulse(string refName, double pulseVoltage, bool check_error = false);

        void SetClockFrequencyAndIdealDelay(SUPPORTEDFORMAT format, double bandwidth);//(bool isWLAN, bool is20MHzor40MHz);
        void SetClockFrequencyForFlexSampleRate(double sampleRate, double OSR);
        void SetSourceType(bool SourceisVXT);

        void UpdateFixedDelay(string reference, double etSampleRate, double delay);


    }
    public interface IAWG2 : IAWG
    {
        #region Added for 5G
        bool LoadWaveform(string srcFile, string dstName, string channelName = "");
        bool Clear(string waveSegName);
        bool SetSampleRate(ChannelEnum channel, double sampleRate);
        bool SetChannelEnable(ChannelEnum channel, bool enable);
        bool PlayWaveformWithIQChannels();

        //Add for M8195A/M8190A interface functions
        bool LoadIQWaveforminDualMode(string srcFil);
        bool SetChannelOffSet(int channelNumber, double Offset);
        bool SetSampleRate(double sampleRate);
        bool PlayIQWaveform(int I_channel, int Q_channel);
        #endregion
    }

    public interface IVSG : IInstrument
    {
        #region Properties
        double Frequency { get; set; }
        double Amplitude { get; set; }
        double BasebandPower { get; set; }
        double BasebandFrequency { get; set; }
        bool OutputEnabled { get; set; }
        bool ModulationEnabled { get; set; }
        bool ALCEnabled { get; set; }
        bool WideBandEnabled { get; set; }

        bool BlankRFDuringTuning { get; set; }
        Source_SynchronizationTriggerTypeEnum SyncTrigType { get; set; }

        string SelectedWaveform { get; set; }
        List<string> WaveformList { get; set; }
        ArbInfo Arb { get; set; }
        object VSG_ { get; set; }
        #endregion

        #region Interface Functions
        bool Apply();
        bool SetupSource(double freq, double ampl, bool enableOutput, PortEnum port, bool apply = true);
        bool SetupTrigger(bool extTriggerEnabled = false, bool syncOutputTriggerEnabled = true, SourceTriggerEnum extTrigSource = SourceTriggerEnum.SourceTriggerPXITrigger2, 
                          SourceTriggerEnum syncOutputTriggerDest = SourceTriggerEnum.SourceTriggerPXITrigger2, double syncOutputTriggerPulseWidth = 10e-6, bool apply = true);
        bool StopModulation();
        bool SetIQDelay(double delay, bool apply = true);
        bool SetIQScale(double scale, bool apply = true);

        bool GetArbInfo(string refName, ref ArbInfo arbInfo);
        bool EditArbInfo(string refName, ArbInfo arbInfo);
        bool LoadARB(string filePath, string fileName, double startTime = 0, double timeToLoad = 0);
        bool LoadARBDoubles(string ref_name, ref double[] iq_data, double sample_rate, double rms, double scale_factor);
        bool PlayARB(string waveform, double shortTime = 0.5, bool bShortWaveform = false);
        bool PlayByTriggerEvent(string waveform, int trigger_number, double delay, bool continous);
        bool PlaySequence(string waveform);
        bool GetARBCatalog();
        bool GeneratePowerRampArb(string refName, double minPower, double maxPower, double duration, double sampleRate);
        bool GeneratePulseArbSequence(string refName, double leadTime, double dutyCycle);
        bool GeneratePulseArbSequenceForDynamicEVM(string refName, double dTriggerDelay, double dDutyCycle);

        //bool RunDcOffsetCal(double[] freqs, string waveform);
        //bool DoPowerSearch(ref double rfOffset, ref double scaleOffset);
        //bool UsePowerSearchResult(double rfOffset, double scaleOffset);
        bool WaitUntilSettled(int miliseconds);

        double GetWaveformTime(string refName);
        double GetSyncOffsetOfThisPowerOn();

        bool P2PPrepareVSG(P2PTransferType xferType, string refName, ref int dataFormat, ref long numBytes);
        bool P2PTransfer(P2PTransferType xferType, int peerSessionId, short peerPortSpace, long peerPortOffset, long transferLength, ref int jobId);

        #endregion
    }

    public interface IVSA : IInstrument
    {
        #region Properties
        double Frequency { get; set; }
        double Power { get; set; }
        object VSA_ { get; set; }
        //bool xAppState { get; set; }
        string xAppAddress { get; set; }
        AnalyzerAcquisitionModeEnum AnalyzerAcquisitionMode { get; set; }
        AnalyzerFFTWindowShapeEnum AnalyzerFFTWindowShape { get; set; }
        AnalyzerFFTAcquisitionLengthEnum AnalyzerFFTLength { get; set; }
        AnalyzerTriggerEnum AnalyzerTriggerSource { get; set; }
        AnalyzerConversionEnum AnalyzerRfConversion { get; set; }
        double AnalyzerIFBandwidth { get; set; }
        double PowerAcqOffsetFreq { get; set; }
        double PowerAcqBandWidth { get; set; }
        double PowerAcqDuration { get; set; }
        double WindowENBW { get; set; }
        TestParameter INIInfo { get; set; }
        TestResultInfo TSInfo { get; set; }
        PowerServoInfo PSInfo { get; set; }
        IQDataInfo IQInfo { get; set; }
        #endregion

        #region Interface Functions
        bool Apply();
        bool Arm();

        bool SetupReceiver(
            AnalyzerAcquisitionModeEnum acquisitionMode,
            double frequency,
            double power,
            AnalyzerFFTAcquisitionLengthEnum fftAcquisitionLength,
            AnalyzerFFTWindowShapeEnum fftWindowShape,
            PortEnum port,
            bool apply = true);
        bool SetupTrigger(
            AnalyzerTriggerModeEnum triggerMode = AnalyzerTriggerModeEnum.AcquisitionTriggerModeImmediate,
            double triggerDelay = 0,
            AnalyzerTriggerTimeoutModeEnum timeoutMode = AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
            int timeout = 100,
            AnalyzerTriggerEnum extTriggerSource = AnalyzerTriggerEnum.TriggerPXITrigger2,
            bool apply = true);
        bool SetRms(double rms, bool apply = true);

        bool SetTriggerStatus(AnalyzerTriggerModeEnum mode, double trigDelay, bool apply = true);
        bool GetIQData(double duration, bool applyChanFilter);
        bool GetIQData(ref double[] IQData, double sampling_rate, double duration, bool applyChanFilter = false);
        bool GetPowerSpectrum(ref double[] spectrumData, ref bool bOverloaded, ref double fStart, ref double fStep);

        bool PowerAcqChanFilterConfig(AnalyzerChannelFilterShapeEnum chanFilter, double filterAlpha, double filterBW);
        bool WaitUntilSettled(int timeout);
        bool CalibrationAlign(AnalyzerAlignmentTypeEnum type);

        bool MeasurePOut(double outputLoss, ref bool overload, ref double pOut, bool fftServo);
        bool MeasureStdAcpr(double outputLoss, double channelPower, ref double[] ACPRArray, bool fftServo);
        bool MeasureSpecHarms(bool fftServo, bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);
        bool MeasureFFTHarms(bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);

        bool MeasurePwrHarms(bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);
        bool MeasureGainComp(double duration, double minPower, double arbSampleRate, double inputPower, double dutOutputLoss, ref GainCompressionResult result);
        bool MeasureIM3(double dutOutputLoss, double dToneSpace, double dSpan, double dRBW, bool bIM3OptMode, ref double[] IM3DataArray);
        bool MeasureIM3OfModulation(double dutOutputLoss, double dBandWidth, double dToneSpace, double dFilterBW, bool bIM3OptMode, ref double[] IM3DataArray);
        bool SetupReceiverForNPRP();
        bool ReadMagnitudeDataForNPRP(int CaptureID, ref double[] Data, ref bool Overload);

        bool MeasureGsmAcpr(int numAverages, bool fftServo, ref double[] ACPRArray);
        bool MeasureLteAcpr(double outputLoss, double channelPower, ref double[] ACPRArray, bool fftServo = true);

        //bool LoadINITestInfo(SUPPORTEDTECHNOLOGIES technology, string iniPath = null, string waveformPath = null);
        bool P2PPrepareVSA(P2PTransferType xferType, double duration, ref int numSamples, double arbSampleRate,
                ref int dataFormat, ref double scaleFactor, ref bool overload, ref long numBytes);
        bool P2PTransfer(P2PTransferType xferType, int peerSessionId, short peerPortSpace, long peerPortOffset, long transferLength, ref int jobId);
        #endregion
    }

    public interface IVXT : IVSA, IVSG
    {
        #region New Properties
        PortEnum SourceOutputPort { get; set; }
        PortEnum ReceiverInputPort { get; set; }
        object VXT_ { get; set; }
        string VxtModelNumber { get; set; }
        string VxtSerialNumber { get; set; }
        #endregion

        #region New Interface Functions
        new bool Apply();
        new bool Arm();
        new bool SetupSource(double freq = 1700, double ampl = -10, bool enableOutput = true, PortEnum port = PortEnum.PortRFOutput, bool apply = true);
        new bool SetupReceiver(
            AnalyzerAcquisitionModeEnum acquisitionMode,
            double frequency,
            double power,
            AnalyzerFFTAcquisitionLengthEnum fftAcquisitionLength,
            AnalyzerFFTWindowShapeEnum fftWindowShape,
            PortEnum port,
            bool apply = true);
            bool SetupSrcTrigger(bool externalTriggerEnabled = false, bool enabled = true,
                SourceTriggerEnum extTriggerSource = SourceTriggerEnum.SourceTriggerPXITrigger2, 
                SourceTriggerEnum syncOutputTriggerDest = SourceTriggerEnum.SourceTriggerPXITrigger2,
                double pulse_width = 10e-6, 
                SourceTriggerModeEnum mode = SourceTriggerModeEnum.SourceTriggerModePulse,
                SourceTriggerPolarityEnum polarity = SourceTriggerPolarityEnum.SourceTriggerPolarityPositive, 
                bool apply = true);
            bool SetupRcvTrigger(AnalyzerTriggerModeEnum mode, double trigDelay = 0, 
                AnalyzerTriggerTimeoutModeEnum timeoutMode = AnalyzerTriggerTimeoutModeEnum.TriggerTimeoutModeAutoTriggerOnTimeout,
                int timeout = 100, 
                AnalyzerTriggerEnum extTriggerSource = AnalyzerTriggerEnum.TriggerPXITrigger2, 
                bool apply = true);
            bool SetupRcvTriggerStatus(AnalyzerTriggerModeEnum mode, double trigDelay, bool apply = true);

        new bool StopModulation();
        new bool GetArbInfo(string refName, ref ArbInfo arbInfo);
        new bool EditArbInfo(string refName, ArbInfo arbInfo);
        new bool LoadARB(string filePath, string fileName, double startTime = 0, double timeToLoad = 0);
        new bool PlayARB(string waveform, double shortTime = 0.5, bool bShortWaveform = false);
        new bool PlayByTriggerEvent(string waveform, int trigger_number, double delay, bool continous);
        new bool PlaySequence(string waveform);
        new bool GetARBCatalog();
        new bool GeneratePowerRampArb(string refName, double minPower, double maxPower, double duration, double sampleRate);
        new bool GeneratePulseArbSequence(string refName, double leadTime, double dutyCycle);
        new bool GeneratePulseArbSequenceForDynamicEVM(string refName, double dTriggerDelay, double dDutyCycle);
        new bool GetIQData(double duration, bool applyChanFilt = false);
        new bool GetIQData(ref double[] IQData, double sampling_rate, double duration, bool applyChanFilt = false);

        new bool MeasurePOut(double outputLoss, ref bool overload, ref double pOut, bool fftServo = true);
            bool MeasureStdAcpr(ref double[] ACPRArray);
        //new bool MeasureSpecHarms(bool fftServo, bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);
        //new bool MeasureFFTHarms(bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);
        //new bool MeasurePwrHarms(bool[] harmsToMeasure, ref double[] harmonicsDataArray, double outputLoss, HARMONICS_RESULT_FORMAT rf);
            bool ServoInputPower(double pIn, double pOut, double poutMargin, bool test_ps, bool test_acpr, bool test_harm, AnalyzerAcquisitionModeEnum harmMode, bool[] harmsToTest,
                ref bool servoPass, ref bool overLoad, ref double channelPower, ref int servoCount, ref double[] acpr_result, ref double[] harmDataArray, ref double measure_time);
        new bool MeasureGainComp(double duration, double minPower, double arbSampleRate, double inputPower, double dutOutputLoss, ref GainCompressionResult result);
        new bool MeasureIM3(double dutOutputLoss, double dToneSpace, double dSpan, double dRBW, bool bIM3OptMode, ref double[] IM3DataArray);
        new bool MeasureIM3OfModulation(double dutOutputLoss, double dBandWidth, double dToneSpace, double dFilterBW, bool bIM3OptMode, ref double[] IM3DataArray);
        new bool SetupReceiverForNPRP();
        new bool ReadMagnitudeDataForNPRP(int CaptureID, ref double[] Data, ref bool Overload);
            bool MeasureGsmAcpr(int numAverages);
        new bool MeasureLteAcpr(double outputLoss, double channelPower, ref double[] ACPRArray, bool fftServo = true);
            bool LoadINITestInfo(string technology, string iniPath, string waveformPath);

        new bool P2PPrepareVSG(P2PTransferType xferType, string refName, ref int dataFormat, ref long numBytes);
        new bool P2PPrepareVSA(P2PTransferType xferType, double duration, ref int numSamples, double arbSampleRate,
                               ref int dataFormat, ref double scaleFactor, ref bool overload, ref long numBytes);
        new bool P2PTransfer(P2PTransferType xferType, int peerSessionId, short peerPortSpace, long peerPortOffset, long transferLength, ref int jobId);

        new double GetWaveformTime(string refName);
        new double GetSyncOffsetOfThisPowerOn();
        #endregion
    }

    public interface ISWITCH : IInstrument
    {
        bool SwitchSetPath(string path);
        bool SwitchSetPath(int pathId);
        bool SetConfigFile(string config_filename);
        List<string> GetAvailablePaths();
    }

    public interface ICFR
    {
        #region Properties
        bool cfrEnabled { set; get; }
        double targetPapr { set; get; }
        bool cfrPeakWindow { set; get; }
        #endregion

        #region Methods
        void ApplyCfr(object input, out object output);
        void SetCfrParameters(bool cfr_enabled = false, double target_papr = 0.0, bool cfr_peak_window = true);
        #endregion
    }

    public interface IDPD
    {
        #region Properties
        bool dpdEnabled { set; get; }
        DpdModelType dpdModel { set; get; }
        WaveformSourceType dpdControl { set; get; }
        UserDefinedModelType udefType { set; get; }
        double dpdEvalTime { set; get; }
        double dpdTrigDelay { set; get; }
        int lutSize { set; get; }
        int amamPoly { set; get; }
        int ampmPoly { set; get; }
        bool MemModelQr { set; get; }
        int memOrder { set; get; }
        int nonLin { set; get; }
        int crossTerm { set; get; }
        bool oddOnly { set; get; }
        string dpdModelFile { set; get; }
        #endregion

        #region Methods
        void SetDpdParameters(
            bool dpd_enabled = false,
            WaveformSourceType dpd_control = WaveformSourceType.PreDefined,
            DpdModelType dpd_model = DpdModelType.LUT,
            UserDefinedModelType udef_type = UserDefinedModelType.UserDefined_Function,
            double dpd_eval_time = 1e-6,
            double dpd_trig_delay = 1e-6,
            int lut_size = 1,
            int amam_poly = 1,
            int ampm_poly = 1,
            bool mem_model_qr = true,
            int mem_order = 1,
            int non_linear = 1,
            int cross_term = 1,
            bool odd_only = false,
            string dpd_model_file = "lut.csv"
            );

        void ExtractDpdModel(object reference, object measured, out object predistored, ref object lut_or_coef, bool saveModel);
        void ApplyDpdModel(object predistorted);
        void SaveLutModelToFile(object lut_model, string csv_filename);
        void LoadLutModelFromFile(ref object lut_model, string csv_filename);
        double CalcDeltaEvm(object reference, object measured);
        double GetAmamAmpmData(object reference, object measured, ref Array am_x, ref Array am_y, ref Array pm_y);
        void CreateMeasuredData(double duration, bool applyChanFilter, ref object measured);

        #endregion
    }

    public interface IET
    {
        #region Properties
        bool etEnabled { set; get; }
        double etSampleRate { set; get; }
        int envOsr { set; get; }
        int fixedOsr { set; get; }
        EnvInputType envInputType { set; get; }
        double envArbRfOver { set; get; }
        string shapingTableFile { set; get; }
        string etArbModel { set; get; }
        double Vcm { set; get; }
        double Vref { set; get; }
        double etpsGain { set; get; }
        bool enableFlexSampleRate { set; get; }
        #endregion

        #region Methods
        void SetETParameters(
            bool isWLAN = false,
            bool et_enabled = false,
            double et_sample_rate = 245.76e6,
            EnvInputType env_inputtype = EnvInputType.ABSOLUTE_RF,
            double env_arb_rf_over = 0,
            string shaping_table_file = null,
            string et_arb_model = "Signadyne",
            WaveformSourceType model_control = WaveformSourceType.PreDefined,
            UserFileType user_file_type = UserFileType.SignalStudio,
            double user_sample_rate = 122.88e6,
            double vcm = 0,
            double vref = 0,
            double etps_gain = 0,
            bool is_flex_samplerate = false
            );

        void GenerateEnvelope(object handle);
        double EtAlign(string method, ACPR_FORMAT_TYPE format,
            bool do_powerservo,
            double freq = 1700e6,
            double targetPout = -20, double targetGain = 10, double poutMargin = 0.5,
            bool fftServo = true, int loopCount = 10,
            double start = -250e-9, double stop = 250e-9, double step_1 = 10e-9, double step_2 = 1e-9);
        void GetAlignData1(ref object x, ref object y1, ref object y2);
        void GetAlignData2(ref object x, ref object y1, ref object y2);
        #endregion
    }

    public interface IDPDET : ICFR, IDPD, IET
    {
        void SetPAParameters(double p_out, double gain);
        void GenerateRFWaveform();
    }

    public interface ISigStudio : IInstrument, IDPDET
    {
        #region Properties

        #region VSG, VSA, AWG, IVXT
        IVSG vsg { get; set; }
        IVSA vsa { get; set; }
        IAWG awg { get; set; }
        IVXT vxt { get; set; }
        #endregion

        #region PA Parameters
        double targetPout { set; get; }
        double targetGain { set; get; }
        #endregion

        #region DPD/ET Control Parameter
        bool enableUserWaveform { set; get; }
        string userDpdFile { set; get; }
        bool useDpdForEnv { set; get; }
        double sigStudioWaveformLength { set; get; }
        double dpdBandwidth { set; get; }
        int iqOsr { set; get; }
        #endregion

        #region User Specified Waveform File Parameter
        UserFileType userFileType { set; get; }
        double userSampleRate { set; get; }
        double userBandwidth { set; get; }
        string userSignalType { set; get; }
        #endregion

        #region Processed File Names
        string sigStudioTempFilePath { set; get; }
        string sigStudioFilePath { set; get; }
        string iqCfFileName { set; get; }
        string etCfFileName { set; get; }
        string iqDpdFileName { set; get; }
        string etDpdFileName { set; get; }
        #endregion

        #region Data Handles
        object reference { get; set; }
        object predistorted { get; set; }
        object measured { get; set; }
        object lut { get; set; }
        object coefficient { get; set; }
        #endregion

        #endregion

        #region Methods
        object GetApi();
        void SetAllParameters(string testWaveform);
        void SetWaveformParameters(string testWaveform, UserFileType waveformType, double sampleRate, int iqOsr, double dpdBandwidth);
        bool SaveWaveformToFile(object handle, string wfm_filename);
        bool LoadWaveformFromFile(object handle, string wfm_filename);
        #endregion
    }

    public interface IUserDefinedDPD
    {
        string user_defined_dll_file { get; set; }
        string user_defined_dll_classname { get; set; }
        /// <summary>
        /// GetAmAmAmPmFunc(Complex[] reference_IQ, Complex[] measured_IQ, ref Array am_x, ref Array am_y, ref Array pm_y); 
        /// </summary>
        string user_defined_get_amam_ampm_func { get; set; }
        /// <summary>
        /// ExtractDpdModelFunc(Complex[] reference_IQ, Complex[] measured_IQ, out Complex[] predistorted, ref Complex[] lut_or_coef); 
        /// </summary>
        string user_defined_extract_dpd_model_func { get; set; }
    }

    public interface IOscilloscope : IInstrument
    {
        byte[] GetScreenData();
        void AutoScale();

        void ClearScreen();
        void SendCommand(string cmd);
    }
    public interface IDigitizer : IInstrument
    {
    }
    #endregion

}
