
#region hardware settings

//#define TRUEALAZAR
#undef TRUEALAZAR

//#define TRUEDAQ
#undef TRUEDAQ

//#define TRUEIMAQ
#undef TRUEIMAQ

#define TRUECUDA
//#undef TRUECUDA

#define TRUEIPP
//#undef TRUEIPP

#endregion  // hardware settings


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NationalInstruments;
using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Conversion;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.Dsp.Filters;
using NationalInstruments.Analysis.Math;
using NationalInstruments.Analysis.Monitoring;
using NationalInstruments.Analysis.SignalGeneration;
using NationalInstruments.Analysis.SpectralMeasurements;
using NationalInstruments.Controls;
using NationalInstruments.Controls.Rendering;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.Visa;
using NationalInstruments.DAQmx;
using Task = NationalInstruments.DAQmx.Task;

using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using System.Windows.Forms;
using System.Reflection;


#region more hardware settings

#if TRUEALAZAR
#endif  // TRUEALAZAR

#if TRUEDAQ
#endif  // TRUEDAQ

#if TRUEIMAQ
#endif  // TRUEIMAQ

#if TRUECUDA
#endif  // TRUECUDA

#if TRUEIPP
#endif  // TRUEIPP

#endregion  // more hardware settings



namespace nOCT
{

    public partial class MainWindow : Window
    {

        #region define semi-globals

        private CUIData UIData;
        LinkedList<CDataNode> nodeList = new LinkedList<CDataNode>();
        private CThreadData threadData = new CThreadData();
        DispatcherTimer timerUIUpdate = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };

        #if TRUECUDA
        nOCTcudaWrapper cudaWrapper;
        #endif  // TRUECUDA

        #if TRUEIPP
        nOCTippWrapper ippWrapper;
        #endif  // TRUEIPP

        #if TRUEIMAQ
        nOCTimaqWrapper imaqWrapper;
        #endif  // TRUEIMAQ

        #endregion define semi-globals


        public MainWindow()
        {

            InitializeComponent();

            #region initialize UI

            UIData = new CUIData { };
            this.DataContext = UIData;
            InitializeUI();

            #endregion initialize UI

            #region start UI update timer

            timerUIUpdate.Tick += new EventHandler(UIUpdate);
            timerUIUpdate.IsEnabled = true;

            #endregion start UI update timer

            GC.Collect();

        }   // MainWindow


        private void InitializeUI()
        {

            #region populate list of computation devices

            cbLLCUDADevice.Items.Add("NI");  // index = 0
            cbLLCUDADevice.Items.Add("IPP");  // index = 1

            #if TRUECUDA // (index for cuda devices starts at 2)
            int nDeviceCount = 0;
            StringBuilder strDeviceName = new StringBuilder(256);
            int nRet = nOCTcudaWrapper.getDeviceCount(ref nDeviceCount);
            for (int nDevice = 0; nDevice < nDeviceCount; nDevice++)
            {
                nRet = nOCTcudaWrapper.getDeviceName(nDevice, strDeviceName);
                cbLLCUDADevice.Items.Add(nDevice + ":" + strDeviceName);
            }   // for (int nDevice
            #endif  // TRUECUDA

            #endregion populate list of computation devices


            #region load last parameter file (if it exists)

            if (File.Exists("lastparameterfilename.txt"))
            {
                string line;
                StreamReader lastParameter = new StreamReader("lastparameterfilename.txt");
                line = lastParameter.ReadLine();
                lastParameter.Close();
                if (File.Exists(line))
                {
                    UIData.strLLConfigurationFilename = line;
                    LoadParameterFile(line);
                }   // if (File.Exists(line
            }   // if (File.Exists("lastparameterfilename.txt"

            #endregion load last parameter file (if it exists)


            #region set up linked list display

            UIData.pnLinkedList = new int[2, 2];
            Array.Clear(UIData.pnLinkedList, 0, UIData.pnLinkedList.Length);
            graphLRDiagnostics.DataSource = UIData.pnLinkedList;

            #endregion set up linked list display

            UIData.nConfigState = 0;

        }   // private void InitializeUI


        void LoadParameterFile(string strFile)
        {
            StreamReader sr = new StreamReader(strFile);
            string line = sr.ReadLine();
            while (line != null)
            {
                int n1 = line.IndexOf("`");
                int n2 = line.IndexOf("'");
                if (n1 != -1)
                {
                    string parametername = line.Substring(0, n1 - 1);
                    string parametervalue = line.Substring(n1 + 1, n2 - n1 - 1);

                    #region template

                    if (parametername == UIData.name_strXXX) UIData.strXXX = parametervalue;
                    if (parametername == UIData.name_nXXX) UIData.nXXX = int.Parse(parametervalue);
                    if (parametername == UIData.name_bXXX) UIData.bXXX = bool.Parse(parametervalue);
                    if (parametername == UIData.name_fXXX) UIData.fXXX = float.Parse(parametervalue);

                    #endregion  // template

                    #region UL

                    if (parametername == UIData.name_nULDisplayIndex) UIData.nULDisplayIndex = int.Parse(parametervalue);
                    if (parametername == UIData.name_nULTop) UIData.nULTop = int.Parse(parametervalue);
                    if (parametername == UIData.name_nULLeft) UIData.nULLeft = int.Parse(parametervalue);

                    if (parametername == UIData.name_nULDAQChannel) UIData.nULDAQChannel = int.Parse(parametervalue);
                    if (parametername == UIData.name_fULDAQMax) UIData.fULDAQMax = float.Parse(parametervalue);
                    if (parametername == UIData.name_fULDAQMin) UIData.fULDAQMin = float.Parse(parametervalue);

                    if (parametername == UIData.name_bULRawDataShowCalibration) UIData.bULRawDataShowCalibration = bool.Parse(parametervalue);
                    if (parametername == UIData.name_fULRawDataMax) UIData.fULRawDataMax = float.Parse(parametervalue);
                    if (parametername == UIData.name_fULRawDataMin) UIData.fULRawDataMin = float.Parse(parametervalue);

                    if (parametername == UIData.name_nULIntensityChooser) UIData.nULIntensityChooser = int.Parse(parametervalue);
                    if (parametername == UIData.name_fULIntensityMax) UIData.fULIntensityMax = float.Parse(parametervalue);
                    if (parametername == UIData.name_fULIntensityMin) UIData.fULIntensityMin = float.Parse(parametervalue);

                    #endregion  // UL

                    #region UR

                    if (parametername == UIData.name_nURDisplayIndex) UIData.nURDisplayIndex = int.Parse(parametervalue);
                    if (parametername == UIData.name_nURTop) UIData.nURTop = int.Parse(parametervalue);
                    if (parametername == UIData.name_nURLeft) UIData.nURLeft = int.Parse(parametervalue);

                    if (parametername == UIData.name_nURIntensityCamera) UIData.nURIntensityCamera = int.Parse(parametervalue);
                    if (parametername == UIData.name_nURIntensityLines) UIData.nURIntensityLines = int.Parse(parametervalue);
                    if (parametername == UIData.name_fURIntensityMax) UIData.fURIntensityMax = float.Parse(parametervalue);
                    if (parametername == UIData.name_fURIntensityMin) UIData.fURIntensityMin = float.Parse(parametervalue);

                    #endregion  // UR

                    #region LL

                    if (parametername == UIData.name_strLLConfigurationFilename) UIData.strLLConfigurationFilename = parametervalue;

                    if (parametername == UIData.name_strLLAlazarBoard) UIData.strLLAlazarBoard = parametervalue;
                    if (parametername == UIData.name_bLLAlazarCh1) UIData.bLLAlazarCh1 = bool.Parse(parametervalue);
                    if (parametername == UIData.name_bLLAlazarCh2) UIData.bLLAlazarCh2 = bool.Parse(parametervalue);
                    if (parametername == UIData.name_nLLAlazarLineLength) UIData.nLLAlazarLineLength = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLAlazarSamplingRate) UIData.nLLAlazarSamplingRate = int.Parse(parametervalue);

                    if (parametername == UIData.name_strLLDAQDevice) UIData.strLLDAQDevice = parametervalue;

                    if (parametername == UIData.name_strLLIMAQParallel) UIData.strLLIMAQParallel = parametervalue;
                    if (parametername == UIData.name_strLLIMAQPerpendicular) UIData.strLLIMAQPerpendicular = parametervalue;
                    if (parametername == UIData.name_nLLIMAQLineLength) UIData.nLLIMAQLineLength = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLIMAQRingBuffers) UIData.nLLIMAQRingBuffers = int.Parse(parametervalue);

                    if (parametername == UIData.name_nLLSystemType) UIData.nLLSystemType = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLSystemActual) UIData.nLLSystemActual = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLLineRate) UIData.nLLLineRate = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLLinesPerChunk) UIData.nLLLinesPerChunk = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLChunksPerImage) UIData.nLLChunksPerImage = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLImagesPerVolume) UIData.nLLImagesPerVolume = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLLinkedListLength) UIData.nLLLinkedListLength = int.Parse(parametervalue);

                    if (parametername == UIData.name_nComputationDevice) UIData.nComputationDevice = int.Parse(parametervalue);

                    if (parametername == UIData.name_strLLFileDirectory) UIData.strLLFileDirectory = parametervalue;
                    if (parametername == UIData.name_strLLFilePrefix) UIData.strLLFilePrefix = parametervalue;
                    if (parametername == UIData.name_nLLFileNumber) UIData.nLLFileNumber = int.Parse(parametervalue);
                    if (parametername == UIData.name_bLLFileRecord) UIData.bLLFileRecord = bool.Parse(parametervalue);
                    if (parametername == UIData.name_nLLFileCycle) UIData.nLLFileCycle = int.Parse(parametervalue);

                    if (parametername == UIData.name_fLLCenterX) UIData.fLLCenterX = float.Parse(parametervalue);
                    if (parametername == UIData.name_fLLCenterY) UIData.fLLCenterY = float.Parse(parametervalue);
                    if (parametername == UIData.name_fLLFastAngle) UIData.fLLFastAngle = float.Parse(parametervalue);
                    if (parametername == UIData.name_fLLRangeFast) UIData.fLLRangeFast = float.Parse(parametervalue);
                    if (parametername == UIData.name_fLLRangeSlow) UIData.fLLRangeSlow = float.Parse(parametervalue);
                    if (parametername == UIData.name_nLLDwellFast) UIData.nLLDwellFast = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLDwellSlow) UIData.nLLDwellSlow = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLLRounding) UIData.nLLRounding = int.Parse(parametervalue);
                    if (parametername == UIData.name_fLLPolModLow) UIData.fLLPolModLow = float.Parse(parametervalue);
                    if (parametername == UIData.name_fLLPolModHigh) UIData.fLLPolModHigh = float.Parse(parametervalue);

                    #endregion  // LL

                    #region LR

                    #region output tab

                    if (parametername == UIData.name_bLROutput) UIData.bLROutput = bool.Parse(parametervalue);

                    if (parametername == UIData.name_nOutputDigitalClockType) UIData.nOutputDigitalClockType = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputP0L0Type) UIData.nOutputP0L0Type = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputP0L1Type) UIData.nOutputP0L1Type = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputP0L2Type) UIData.nOutputP0L2Type = int.Parse(parametervalue);

                    if (parametername == UIData.name_nOutputAnalogClockType) UIData.nOutputAnalogClockType = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputAO0Type) UIData.nOutputAO0Type = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputAO1Type) UIData.nOutputAO1Type = int.Parse(parametervalue);
                    if (parametername == UIData.name_nOutputAO2Type) UIData.nOutputAO2Type = int.Parse(parametervalue);

                    #endregion output tab

                    #region processing tab

                    if (parametername == UIData.name_bLRReferenceActive) UIData.bLRReferenceActive = bool.Parse(parametervalue);
                    if (parametername == UIData.name_nLRReferenceMethod) UIData.nLRReferenceMethod = int.Parse(parametervalue);
                    if (parametername == UIData.name_nLRReferenceDisplay) UIData.nLRReferenceDisplay = int.Parse(parametervalue);

                    if (parametername == UIData.name_nFFTMaskLeft) UIData.nFFTMaskLeft = int.Parse(parametervalue);
                    if (parametername == UIData.name_nFFTMaskRight) UIData.nFFTMaskRight = int.Parse(parametervalue);
                    if (parametername == UIData.name_nFFTMaskRound) UIData.nFFTMaskRound = int.Parse(parametervalue);

                    #endregion processing tab

                    #region calibration tab

                    if (parametername == UIData.name_strCalibrationFile) UIData.strCalibrationFile = parametervalue;
                    if (parametername == UIData.name_bCalibrationActive) UIData.bCalibrationActive = bool.Parse(parametervalue);
                    if (parametername == UIData.name_nCalibrationDepthLeft) UIData.nCalibrationDepthLeft = int.Parse(parametervalue);
                    if (parametername == UIData.name_nCalibrationDepthRight) UIData.nCalibrationDepthRight = int.Parse(parametervalue);
                    if (parametername == UIData.name_nCalibrationDepthRound) UIData.nCalibrationDepthRound = int.Parse(parametervalue);
                    if (parametername == UIData.name_nCalibrationPhaseLeft) UIData.nCalibrationPhaseLeft = int.Parse(parametervalue);
                    if (parametername == UIData.name_nCalibrationPhaseRight) UIData.nCalibrationPhaseRight = int.Parse(parametervalue);

                    #endregion calibration tab

                    #region dispersion tab

                    if (parametername == UIData.name_strDispersionFile) UIData.strDispersionFile = parametervalue;
                    if (parametername == UIData.name_bDispersionActive) UIData.bDispersionActive = bool.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionLine) UIData.nDispersionLine = int.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionDepthLeft) UIData.nDispersionDepthLeft = int.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionDepthRight) UIData.nDispersionDepthRight = int.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionDepthRound) UIData.nDispersionDepthRound = int.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionPhaseLeft) UIData.nDispersionPhaseLeft = int.Parse(parametervalue);
                    if (parametername == UIData.name_nDispersionPhaseRight) UIData.nDispersionPhaseRight = int.Parse(parametervalue);

                    #endregion dispersion tab

                    #endregion  // LR

                }   // if (n1
                line = sr.ReadLine();
            }   // while (line
            sr.Close();
        }   // void LoadParameterFile


        void SaveParameterFile(string strFile)
        {

            StreamWriter sw = new StreamWriter(strFile);

            #region template

            sw.WriteLine(UIData.name_strXXX + "=`" + UIData.strXXX + "'");
            sw.WriteLine(UIData.name_nXXX + "=`" + UIData.nXXX + "'");
            sw.WriteLine(UIData.name_bXXX + "=`" + UIData.bXXX + "'");
            sw.WriteLine(UIData.name_fXXX + "=`" + UIData.fXXX + "'");

            #endregion  // template

            #region UL

            sw.WriteLine(UIData.name_nULDisplayIndex + "=`" + UIData.nULDisplayIndex + "'");
            sw.WriteLine(UIData.name_nULTop + "=`" + UIData.nULTop + "'");
            sw.WriteLine(UIData.name_nULLeft + "=`" + UIData.nULLeft + "'");

            sw.WriteLine(UIData.name_nULDAQChannel + "=`" + UIData.nULDAQChannel + "'");
            sw.WriteLine(UIData.name_fULDAQMax + "=`" + UIData.fULDAQMax + "'");
            sw.WriteLine(UIData.name_fULDAQMin + "=`" + UIData.fULDAQMin + "'");

            sw.WriteLine(UIData.name_bULRawDataShowCalibration + "=`" + UIData.bULRawDataShowCalibration + "'");
            sw.WriteLine(UIData.name_fULRawDataMax + "=`" + UIData.fULRawDataMax + "'");
            sw.WriteLine(UIData.name_fULRawDataMin + "=`" + UIData.fULRawDataMin + "'");

            sw.WriteLine(UIData.name_nULIntensityChooser + "=`" + UIData.nULIntensityChooser + "'");
            sw.WriteLine(UIData.name_fULIntensityMax + "=`" + UIData.fULIntensityMax + "'");
            sw.WriteLine(UIData.name_fULIntensityMin + "=`" + UIData.fULIntensityMin + "'");

            #endregion  // UL

            #region UR

            sw.WriteLine(UIData.name_nURDisplayIndex + "=`" + UIData.nURDisplayIndex + "'");
            sw.WriteLine(UIData.name_nURTop + "=`" + UIData.nURTop + "'");
            sw.WriteLine(UIData.name_nURLeft + "=`" + UIData.nURLeft + "'");

            sw.WriteLine(UIData.name_nURIntensityCamera + "=`" + UIData.nURIntensityCamera + "'");
            sw.WriteLine(UIData.name_nURIntensityLines + "=`" + UIData.nURIntensityLines + "'");
            sw.WriteLine(UIData.name_fURIntensityMax + "=`" + UIData.fURIntensityMax + "'");
            sw.WriteLine(UIData.name_fURIntensityMin + "=`" + UIData.fURIntensityMin + "'");

            #endregion  // UR

            #region LL

            sw.WriteLine(UIData.name_strLLConfigurationFilename + "=`" + UIData.strLLConfigurationFilename + "'");
            sw.WriteLine(UIData.name_strLLAlazarBoard + "=`" + UIData.strLLAlazarBoard + "'");
            sw.WriteLine(UIData.name_bLLAlazarCh1 + "=`" + UIData.bLLAlazarCh1 + "'");
            sw.WriteLine(UIData.name_bLLAlazarCh2 + "=`" + UIData.bLLAlazarCh2 + "'");
            sw.WriteLine(UIData.name_nLLAlazarLineLength + "=`" + UIData.nLLAlazarLineLength + "'");
            sw.WriteLine(UIData.name_nLLAlazarSamplingRate + "=`" + UIData.nLLAlazarSamplingRate + "'");
            sw.WriteLine(UIData.name_strLLDAQDevice + "=`" + UIData.strLLDAQDevice + "'");
            sw.WriteLine(UIData.name_strLLIMAQParallel + "=`" + UIData.strLLIMAQParallel + "'");
            sw.WriteLine(UIData.name_strLLIMAQPerpendicular + "=`" + UIData.strLLIMAQPerpendicular + "'");
            sw.WriteLine(UIData.name_nLLIMAQLineLength + "=`" + UIData.nLLIMAQLineLength + "'");
            sw.WriteLine(UIData.name_nLLIMAQRingBuffers + "=`" + UIData.nLLIMAQRingBuffers + "'");
            sw.WriteLine(UIData.name_nLLSystemType + "=`" + UIData.nLLSystemType + "'");
            sw.WriteLine(UIData.name_nLLSystemActual + "=`" + UIData.nLLSystemActual + "'");
            sw.WriteLine(UIData.name_nLLLineRate + "=`" + UIData.nLLLineRate + "'");
            sw.WriteLine(UIData.name_nLLLinesPerChunk + "=`" + UIData.nLLLinesPerChunk + "'");
            sw.WriteLine(UIData.name_nLLChunksPerImage + "=`" + UIData.nLLChunksPerImage + "'");
            sw.WriteLine(UIData.name_nLLImagesPerVolume + "=`" + UIData.nLLImagesPerVolume + "'");
            sw.WriteLine(UIData.name_nLLLinkedListLength + "=`" + UIData.nLLLinkedListLength + "'");
            sw.WriteLine(UIData.name_nComputationDevice + "=`" + UIData.nComputationDevice + "'");
            sw.WriteLine(UIData.name_strLLFileDirectory + "=`" + UIData.strLLFileDirectory + "'");
            sw.WriteLine(UIData.name_strLLFilePrefix + "=`" + UIData.strLLFilePrefix + "'");
            sw.WriteLine(UIData.name_nLLFileNumber + "=`" + UIData.nLLFileNumber + "'");
            sw.WriteLine(UIData.name_bLLFileRecord + "=`" + UIData.bLLFileRecord + "'");
            sw.WriteLine(UIData.name_nLLFileCycle + "=`" + UIData.nLLFileCycle + "'");
            sw.WriteLine(UIData.name_fLLCenterX + "=`" + UIData.fLLCenterX + "'");
            sw.WriteLine(UIData.name_fLLCenterY + "=`" + UIData.fLLCenterY + "'");
            sw.WriteLine(UIData.name_fLLFastAngle + "=`" + UIData.fLLFastAngle + "'");
            sw.WriteLine(UIData.name_fLLRangeFast + "=`" + UIData.fLLRangeFast + "'");
            sw.WriteLine(UIData.name_fLLRangeSlow + "=`" + UIData.fLLRangeSlow + "'");
            sw.WriteLine(UIData.name_nLLDwellFast + "=`" + UIData.nLLDwellFast + "'");
            sw.WriteLine(UIData.name_nLLDwellSlow + "=`" + UIData.nLLDwellSlow + "'");
            sw.WriteLine(UIData.name_nLLRounding + "=`" + UIData.nLLRounding + "'");
            sw.WriteLine(UIData.name_fLLPolModLow + "=`" + UIData.fLLPolModLow + "'");
            sw.WriteLine(UIData.name_fLLPolModHigh + "=`" + UIData.fLLPolModHigh + "'");

            #endregion  // LL

            #region LR

            #region output tab

            sw.WriteLine(UIData.name_bLROutput + "=`" + UIData.bLROutput + "'");

            sw.WriteLine(UIData.name_nOutputDigitalClockType + "=`" + UIData.nOutputDigitalClockType + "'");
            sw.WriteLine(UIData.name_nOutputP0L0Type + "=`" + UIData.nOutputP0L0Type + "'");
            sw.WriteLine(UIData.name_nOutputP0L1Type + "=`" + UIData.nOutputP0L1Type + "'");
            sw.WriteLine(UIData.name_nOutputP0L2Type + "=`" + UIData.nOutputP0L2Type + "'");

            sw.WriteLine(UIData.name_nOutputAnalogClockType + "=`" + UIData.nOutputAnalogClockType + "'");
            sw.WriteLine(UIData.name_nOutputAO0Type + "=`" + UIData.nOutputAO0Type + "'");
            sw.WriteLine(UIData.name_nOutputAO1Type + "=`" + UIData.nOutputAO1Type + "'");
            sw.WriteLine(UIData.name_nOutputAO2Type + "=`" + UIData.nOutputAO2Type + "'");

            #endregion output tab

            #region processing tab

            sw.WriteLine(UIData.name_bLRReferenceActive + "=`" + UIData.bLRReferenceActive + "'");
            sw.WriteLine(UIData.name_nLRReferenceMethod + "=`" + UIData.nLRReferenceMethod + "'");
            sw.WriteLine(UIData.name_nLRReferenceDisplay + "=`" + UIData.nLRReferenceDisplay + "'");
            sw.WriteLine(UIData.name_nFFTMaskLeft + "=`" + UIData.nFFTMaskLeft + "'");
            sw.WriteLine(UIData.name_nFFTMaskRight + "=`" + UIData.nFFTMaskRight + "'");
            sw.WriteLine(UIData.name_nFFTMaskRound + "=`" + UIData.nFFTMaskRound + "'");

            #endregion processing tab

            #region calibration tab

            sw.WriteLine(UIData.name_strCalibrationFile + "=`" + UIData.strCalibrationFile + "'");
            sw.WriteLine(UIData.name_bCalibrationActive + "=`" + UIData.bCalibrationActive + "'");
            sw.WriteLine(UIData.name_nCalibrationDepthLeft + "=`" + UIData.nCalibrationDepthLeft + "'");
            sw.WriteLine(UIData.name_nCalibrationDepthRight + "=`" + UIData.nCalibrationDepthRight + "'");
            sw.WriteLine(UIData.name_nCalibrationDepthRound + "=`" + UIData.nCalibrationDepthRound + "'");
            sw.WriteLine(UIData.name_nCalibrationPhaseLeft + "=`" + UIData.nCalibrationPhaseLeft + "'");
            sw.WriteLine(UIData.name_nCalibrationPhaseRight + "=`" + UIData.nCalibrationPhaseRight + "'");

            #endregion calibration tab

            #region dispersion tab

            sw.WriteLine(UIData.name_strDispersionFile + "=`" + UIData.strDispersionFile + "'");
            sw.WriteLine(UIData.name_bDispersionActive + "=`" + UIData.bDispersionActive + "'");
            sw.WriteLine(UIData.name_nDispersionLine + "=`" + UIData.nDispersionLine + "'");
            sw.WriteLine(UIData.name_nDispersionDepthLeft + "=`" + UIData.nDispersionDepthLeft + "'");
            sw.WriteLine(UIData.name_nDispersionDepthRight + "=`" + UIData.nDispersionDepthRight + "'");
            sw.WriteLine(UIData.name_nDispersionDepthRound + "=`" + UIData.nDispersionDepthRound + "'");
            sw.WriteLine(UIData.name_nDispersionPhaseLeft + "=`" + UIData.nDispersionPhaseLeft + "'");
            sw.WriteLine(UIData.name_nDispersionPhaseRight + "=`" + UIData.nDispersionPhaseRight + "'");

            #endregion dispersion tab

            #endregion  // LR

            sw.Close();
        }   // void SaveParameterFile


        private void btnUpdateUL_Click(object sender, RoutedEventArgs e)
        {
            UIData.bULChange = true;
        }


        private void btnUpdateUR_Click(object sender, RoutedEventArgs e)
        {
            UIData.bURChange = true;
        }


        void UIUpdate(object sender, EventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (UIData.nConfigState == 1)
            {
                #region UL

                #region ranges changed
                if (UIData.bULChange)
                {
                    #region get min and max (with error checking)

                    float fMin = 0.0f;
                    float fMax = 1.0f;

                    switch (UIData.nULDisplayIndex)
                    {
                        case 0:  // raw data
                            fMin = UIData.fULRawDataMin;
                            fMax = UIData.fULRawDataMax;
                            break;
                        case 1:  // intensity
                            fMin = UIData.fULIntensityMin;
                            fMax = UIData.fULIntensityMax;
                            break;
                    }

                    if (fMax <= fMin)
                        fMax = fMin + 1.0f;

                    #endregion get min and max (with error checking)

                    #region set left range
                    axisULLeftHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
                    axisULLeftVertical.Range = new Range<float>(fMin, fMax);
                    #endregion

                    #region set top range
                    axisULTopHorizontal.Range = new Range<int>(0, threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration);
                    axisULTopVertical.Range = new Range<float>(fMin, fMax);
                    #endregion

                    #region set main range
                    axisULMainVertical.Range = new Range<float>(0f, (float)(threadData.n1LineLength));
                    axisULMainHorizontal.Range = new Range<float>(0f, (float)(threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration));
                    ColorScaleMarker[] csMarker = new ColorScaleMarker[2];
                    csMarker[0].Color = Colors.White;
                    csMarker[0].Value = fMin;
                    csMarker[1].Color = Colors.Black;
                    csMarker[1].Value = fMax;
                    csULMain.Markers.RemoveRange(0, 2);
                    csULMain.Markers.AddRange(csMarker);
                    #endregion set main range

                    UIData.bULChange = false;

                }   // if (UIData.bULChange
                #endregion ranges changed

                if (UIData.bULLeftActive)
                    graphULLeft.Refresh();
                if (UIData.bULTopActive)
                    graphULTop.Refresh();
                if (UIData.bULMainActive)
                    graphULMain.Refresh();

                #endregion UL

                #region UR

                #region ranges changed
                if (UIData.bURChange)
                {
                    #region get min and max from selected tab
                    float fMin = 0, fMax = 1;
                    switch (UIData.nURDisplayIndex)
                    {
                        case 0: // none
                            break;
                        case 1: // int
                            fMin = UIData.fURIntensityMin;
                            fMax = UIData.fURIntensityMax;
                            break;
                        case 2: // att
                            break;
                        case 3: // ph
                            break;
                        case 4: // pol
                            break;
                        case 5: // ang
                            break;
                        case 6: // ela
                            break;
                        case 7: // spt
                            break;
                        case 8: // SB
                            break;
                    }  // switch (UIData.nURDisplayIndex
                    #endregion get min and max from selected tab

                    if (fMin > fMax)
                        fMax = fMin + 1.0f;

                    #region set left range
                    axisURLeftHorizontal.Range = new Range<int>(0, threadData.n2LineLength);
                    axisURLeftVertical.Range = new Range<float>(fMin, fMax);
                    #endregion

                    #region set top range
                    axisURTopHorizontal.Range = new Range<int>(0, threadData.n2NumberDepthProfilesPerSet);
                    axisURTopVertical.Range = new Range<float>(fMin, fMax);
                    #endregion

                    #region set main range
                    axisURMainVertical.Range = new Range<float>(0f, (float)(threadData.n2LineLength));
                    axisURMainHorizontal.Range = new Range<float>(0f, (float)(threadData.n2NumberDepthProfilesPerSet));
                    ColorScaleMarker[] csMarker = new ColorScaleMarker[2];
                    csMarker[0].Color = Colors.White;
                    csMarker[0].Value = fMin;
                    csMarker[1].Color = Colors.Black;
                    csMarker[1].Value = fMax;
                    csURMain.Markers.RemoveRange(0, 2);
                    csURMain.Markers.AddRange(csMarker);
                    #endregion

                    UIData.bURChange = false;

                }   // if (UIData.bURChange
                #endregion

                if (UIData.bURLeftActive)
                    graphURLeft.Refresh();
                if (UIData.bURTopActive)
                    graphURTop.Refresh();
                if (UIData.bURMainActive)
                    graphURMain.Refresh();

                #endregion  // UR

                #region LL

                UIData.nLLFileNumber = threadData.nFileNumber;
                UIData.nLLFileCycle = threadData.nFramePosition;

                #endregion  // LL

                #region LR

                #region diagnostic tab

                #region host memory

                ComputerInfo CI = new ComputerInfo();
                UIData.fLRAvailableMemory = Convert.ToSingle(CI.AvailablePhysicalMemory) / 1048576.0f / 1024.0f;

                #endregion  // host memory

                #region linked list

                if ((nodeList.Count() > 1) && (UIData.bLRDiagnostics))
                {
                    threadData.bRecord = UIData.bLLFileRecord;
                    Array.Clear(UIData.pnLinkedList, 0, UIData.pnLinkedList.Length);
                    LinkedListNode<CDataNode> nodeTemp = nodeList.First;
                    for (int nNode = 0; nNode < nodeList.Count; nNode++)
                    {
                        if (nodeTemp.Value.rwls.TryEnterReadLock(0) == true)
                        {
                            UIData.pnLinkedList[nNode, 5] = nodeTemp.Value.nSaved;
                            UIData.pnLinkedList[nNode, 4] = nodeTemp.Value.nAcquired;
                            UIData.pnLinkedList[nNode, 3] = nodeTemp.Value.nProcessed;
                            nodeTemp.Value.rwls.ExitReadLock();
                        }
                        else
                        {  // if (nodeTemp.Value.rwls.TryEnterReadLock
                            UIData.pnLinkedList[nNode, 5] = -1;
                            UIData.pnLinkedList[nNode, 4] = -1;
                            UIData.pnLinkedList[nNode, 3] = -1;
                        }  // if (nodeTemp.Value.rwls.TryEnterReadLock
                        nodeTemp = nodeTemp.Next;
                    }  // for (int nNode
                    if (threadData.nProcess1Node > -1)
                        UIData.pnLinkedList[threadData.nProcess1Node, 2] = 1;
                    if (threadData.nProcess2Node > -1)
                        UIData.pnLinkedList[threadData.nProcess2Node, 1] = 1;
                    graphLRDiagnostics.Refresh();
                }   // if (UIData.nLRLinkedListLength

                #endregion  // linked list

                #region thread status lines

                UIData.strLRMainThreadStatus = threadData.strMainThreadStatus;
                UIData.strLROutputThreadStatus = threadData.strOutputThreadStatus;
                UIData.strLRAcquireThreadStatus = threadData.strAcquireThreadStatus + " (A:" + threadData.strAcquireAlazarThreadStatus + "; D:" + threadData.strAcquireDAQThreadStatus + "; I:" + threadData.strAcquireIMAQThreadStatus + ")";
                UIData.strLRSaveThreadStatus = threadData.strSaveThreadStatus;
                UIData.strLRProcessThreadStatus = threadData.strProcessThreadStatus;
                UIData.strLRProcess1ThreadStatus = threadData.strProcess1ThreadStatus;
                UIData.strLRProcess2ThreadStatus = threadData.strProcess2ThreadStatus;
                UIData.strLRCleanupThreadStatus = threadData.strCleanupThreadStatus;

                #endregion  // thread status lines

                #endregion  // diagnostic tab

                #region signal output

                if (UIData.bLROutput)
                {
                    graphLROutput.Refresh();
                }

                #endregion  // signal output

                #region processing

                if (UIData.bLRReferenceActive)
                {
                    graphReference.Refresh();
                }

                #endregion  // processing

                #region calibration

                if (UIData.bCalibrationActive)
                {
                    graphCalibrationDepthProfile.Refresh();
                    graphCalibrationSpectrum.Refresh();
                    graphCalibrationPhase.Refresh();
                }

                #endregion  // calibration

                #region dispersion

                if (UIData.bDispersionActive)
                {
                    graphDispersionDepthProfile.Refresh();
                    graphDispersionSpectrum.Refresh();
                    graphDispersionPhase.Refresh();
                }

                #endregion  // dispersion

                #endregion  // LR
            }

            watch.Stop();
            var elapsedMS = watch.ElapsedMilliseconds;
            float fWeight = 0.95f;
            UIData.fLRUIUpdateTime = fWeight * UIData.fLRUIUpdateTime + (1.0f - fWeight) * ((float) elapsedMS);

        }   // void UIUpdate


        private void btnLLConfigurationLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                UIData.strLLConfigurationFilename = openFileDialog.FileName;
                LoadParameterFile(openFileDialog.FileName);
                StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                lastParameter.WriteLine(openFileDialog.FileName);
                lastParameter.Close();
            }   // if (openFileDialog.ShowDialog
        }   // btnLLConfigurationLoad_Click


        private void btnLLConfigurationSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                UIData.strLLConfigurationFilename = saveFileDialog.FileName;
                SaveParameterFile(saveFileDialog.FileName);
                StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                lastParameter.WriteLine(saveFileDialog.FileName);
                lastParameter.Close();
            }   // if (saveFileDialog.ShowDialog
        }   // btnLLConfigurationSave_Click


        private void btnLLFileDirectoryBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "file directory";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UIData.strLLFileDirectory = fbd.SelectedPath;
            }   // if (fbd.ShowDialog
        }   // btnLLFileDirectoryBrowse_Click


        private void btnLLGalvoUpdate_Click(object sender, RoutedEventArgs e)
        {
            threadData.mreOutputUpdate.Set();
        }   // btnLLGalvoUpdate_Click


        private void btnLRConfigurationStart_Click(object sender, RoutedEventArgs e)
        {

            #region set up linked list

            #region some local variables

            CDataNode datanode;

            #endregion some local variables


            #region clear linked list

            while (nodeList.Count > 0)
            {
                datanode = nodeList.Last();
                nodeList.RemoveLast();
                datanode = null;
            }   // while (nodeList.Count

            #endregion clear linked list


            #region create first node

            datanode = new CDataNode(UIData, 0);
            nodeList.AddLast(datanode);
            ulong nNodeSize = datanode.nSize;

            #endregion create first node


            #region create the rest of the node list

            for (int nID = 1; nID < UIData.nLLLinkedListLength; nID++)
            {
                datanode = new CDataNode(UIData, nID);
                nodeList.AddLast(datanode);
            }   // for (int nID

            #endregion create the rest of the node list


            #region update UI structures

            UIData.pnLinkedList = new int[nodeList.Count, 8];
            Array.Clear(UIData.pnLinkedList, 0, UIData.pnLinkedList.Length);
            graphLRDiagnostics.DataSource = UIData.pnLinkedList;
            axisLRDiagnosticsHorizontal.Range = new Range<float>(0f, (float)(nodeList.Count));
            axisLRDiagnosticsVertical.Range = new Range<float>(0.5f, 7.5f);

            #endregion update UI structures

            #endregion set up linked list


            #region set up early graph structures

            #region output tab

            int nNumberFrames = UIData.nLLImagesPerVolume;
            int nNumberLines = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;

            UIData.pfOutput = new float[3, nNumberFrames * nNumberLines];
            graphLROutput.DataSource = UIData.pfOutput;

            #endregion output tab

            #endregion set up early graph structures


            #region process1 and process2 structures

            switch (UIData.nLLSystemType)
            {
                case 0: // SD-OCT
                    threadData.rwls1 = new ReaderWriterLockSlim();
                    threadData.n1NumberCalibrationLines = 1;
                    threadData.n1NumberInterferenceLinesPerCalibration = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;
                    threadData.n1LineLength = UIData.nLLIMAQLineLength;
                    threadData.pf1Calibration = new float[threadData.n1NumberCalibrationLines * threadData.n1LineLength];
                    threadData.pf1Interference = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength];
                    threadData.pf1DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    threadData.rwls2 = new ReaderWriterLockSlim();
                    threadData.n2NumberSets = 1;
                    threadData.n2NumberDepthProfilesPerSet = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;
                    threadData.n2LineLength = threadData.n1LineLength >> 1;
                    threadData.pf2Real = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2Imag = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    break;
                case 1: // PS SD-OCT
                    threadData.rwls1 = new ReaderWriterLockSlim();
                    threadData.n1NumberCalibrationLines = 4;
                    threadData.n1NumberInterferenceLinesPerCalibration = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk / 2;
                    threadData.n1LineLength = UIData.nLLIMAQLineLength;
                    threadData.pf1Calibration = new float[threadData.n1NumberCalibrationLines * threadData.n1LineLength];
                    threadData.pf1Interference = new float[2 * threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength];
                    threadData.pf1DAQ = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration];

                    threadData.rwls2 = new ReaderWriterLockSlim();
                    threadData.n2NumberSets = 4;
                    threadData.n2NumberDepthProfilesPerSet = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk / 2;
                    threadData.n2LineLength = threadData.n1LineLength >> 1;
                    threadData.pf2Real = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2Imag = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    // these two only exist for spectral binning code
                    threadData.pf2Calibration = new float[threadData.n1NumberCalibrationLines * threadData.n1LineLength];
                    threadData.pf2Interference = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength];

                    break;
                case 2: // line field
                    threadData.rwls1 = new ReaderWriterLockSlim();
                    threadData.n1NumberCalibrationLines = 1;
                    threadData.n1NumberInterferenceLinesPerCalibration = UIData.nLLIMAQLineLength;
                    threadData.n1LineLength = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;
                    threadData.pf1Calibration = new float[threadData.n1NumberCalibrationLines * threadData.n1LineLength];
                    threadData.pf1Interference = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength];
                    threadData.pf1DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    threadData.rwls2 = new ReaderWriterLockSlim();
                    threadData.n2NumberSets = 1;
                    threadData.n2NumberDepthProfilesPerSet = UIData.nLLIMAQLineLength;
                    threadData.n2LineLength = threadData.n1LineLength >> 1;
                    threadData.pf2Real = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2Imag = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    break;
                case 3: // OFDI
                    threadData.rwls1 = new ReaderWriterLockSlim();
                    threadData.n1NumberCalibrationLines = 1;
                    threadData.n1NumberInterferenceLinesPerCalibration = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;
                    threadData.n1LineLength = UIData.nLLAlazarLineLength;
                    threadData.pf1Calibration = new float[threadData.n1NumberCalibrationLines * threadData.n1LineLength];
                    threadData.pf1Interference = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength];
                    threadData.pf1DAQ = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration];

                    threadData.rwls2 = new ReaderWriterLockSlim();
                    threadData.n2NumberSets = 1;
                    threadData.n2NumberDepthProfilesPerSet = UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk;
                    threadData.n2LineLength = threadData.n1LineLength >> 1;
                    threadData.pf2Real = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2Imag = new float[threadData.n2NumberSets * threadData.n2NumberDepthProfilesPerSet * threadData.n2LineLength];
                    threadData.pf2DAQ = new float[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk];

                    break;
                case 4: // PS OFDI
                    break;
            }   // switch (UIData.nLLSystemType

            #endregion  // process1 and process2 structures


            #region set up main thread

            threadData.Initialize();
            threadData.threadMain = new Thread(MainThread);
            threadData.threadMain.Priority = ThreadPriority.AboveNormal;
            threadData.threadMain.Start();
            threadData.mreMainReady.WaitOne();

            #endregion set up main thread


            #region set up graphs

            #region UL

            #region get min and max (with error checking)

            float fMin = 0.0f;
            float fMax = 1.0f;

            switch (UIData.nULDisplayIndex)
            {
                case 0:  // raw data
                    fMin = UIData.fULRawDataMin;
                    fMax = UIData.fULRawDataMax;
                    break;
                case 1:  // intensity
                    fMin = UIData.fULIntensityMin;
                    fMax = UIData.fULIntensityMax;
                    break;
            }

            if (fMax <= fMin)
                fMax = fMin + 1.0f;

            #endregion get min and max (with error checking)

            UIData.pfULLeft = new float[4, threadData.n1LineLength];
            graphULLeft.DataSource = UIData.pfULLeft;
            axisULLeftHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            axisULLeftVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfULTop = new float[1, threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration];
            graphULTop.DataSource = UIData.pfULTop;
            axisULTopHorizontal.Range = new Range<int>(0, threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration);
            axisULTopVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfULImage = new float[threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration, threadData.n1LineLength];
            graphULMain.DataSource = UIData.pfULImage;
            axisULMainVertical.Range = new Range<float>(0f, (float)(threadData.n1LineLength));
            axisULMainHorizontal.Range = new Range<float>(0f, (float)(threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration));
            ColorScaleMarker[] csMarker = new ColorScaleMarker[2];
            csMarker[0].Color = Colors.White;
            csMarker[0].Value = fMin;
            csMarker[1].Color = Colors.Black;
            csMarker[1].Value = fMax;
            csULMain.Markers.RemoveRange(0, 2);
            csULMain.Markers.AddRange(csMarker);

            #endregion UL

            #region UR

            switch (UIData.nURDisplayIndex)
            {
                case 0: // none
                    break;
                case 1: // int
                    fMin = UIData.fURIntensityMin;
                    fMax = UIData.fURIntensityMax;
                    break;
                case 2: // att
                    break;
                case 3: // ph
                    break;
                case 4: // pol
                    break;
                case 5: // ang
                    break;
                case 6: // ela
                    break;
                case 7: // spt
                    break;
                case 8: // SB
                    break;
            }  // switch (UIData.nURDisplayIndex

            UIData.pfURLeft = new float[4, threadData.n2LineLength];
            graphURLeft.DataSource = UIData.pfURLeft;
            axisURLeftHorizontal.Range = new Range<int>(0, threadData.n2LineLength);
            axisURLeftVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfURTop = new float[1, threadData.n2NumberDepthProfilesPerSet];
            graphURTop.DataSource = UIData.pfURTop;
            axisURTopHorizontal.Range = new Range<int>(0, threadData.n2NumberDepthProfilesPerSet);
            axisURTopVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfURImage = new float[threadData.n2NumberDepthProfilesPerSet, threadData.n2LineLength];
            graphURMain.DataSource = UIData.pfURImage;
            axisURMainVertical.Range = new Range<float>(0f, (float)(threadData.n2LineLength));
            axisURMainHorizontal.Range = new Range<float>(0f, (float)(threadData.n2NumberDepthProfilesPerSet));
            csMarker = new ColorScaleMarker[2];
            csMarker[0].Color = Colors.White;
            csMarker[0].Value = fMin;
            csMarker[1].Color = Colors.Black;
            csMarker[1].Value = fMax;
            csURMain.Markers.RemoveRange(0, 2);
            csURMain.Markers.AddRange(csMarker);

            #endregion UR

            #region LR

            #region processing tab

            UIData.pfReference = new float[4, threadData.n1LineLength];
            graphReference.DataSource = UIData.pfReference;
            axisReferenceHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            fMin = UIData.fULRawDataMin;
            fMax = UIData.fULRawDataMax;
            if (fMax <= fMin)
                fMax = fMin + 1.0f;
            axisReferenceVertical.Range = new Range<float>(fMin, fMax);

            #endregion processing tab

            #region calibration tab

            UIData.pfCalibrationDepthProfile = new float[8, threadData.n2LineLength];
            graphCalibrationDepthProfile.DataSource = UIData.pfCalibrationDepthProfile;
            axisCalibrationDepthHorizontal.Range = new Range<int>(0, threadData.n2LineLength);
            axisCalibrationDepthVertical.Range = new Range<float>(UIData.fULIntensityMin, UIData.fULIntensityMax);

            UIData.pfCalibrationSpectrum = new float[8, threadData.n1LineLength];
            graphCalibrationSpectrum.DataSource = UIData.pfCalibrationSpectrum;
            axisCalibrationSpectrumHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            fMin = UIData.fULRawDataMin;
            fMax = UIData.fULRawDataMax;
            if (fMin >= fMax)
                fMax = fMin + 1.0f;
            axisCalibrationSpectrumVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfCalibrationPhase = new float[5, threadData.n1LineLength];
            graphCalibrationPhase.DataSource = UIData.pfCalibrationPhase;
            axisCalibrationPhaseHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            axisCalibrationPhaseVertical.Range = new Range<float>(-1000f, 1000f);

            UIData.bCalibrationLoad = false;
            UIData.bCalibrationSave = false;
            UIData.bCalibrationClear = false;

            #endregion calibration tab

            #region dispersion tab

            UIData.pfDispersionDepthProfile = new float[2, threadData.n1LineLength];
            graphDispersionDepthProfile.DataSource = UIData.pfDispersionDepthProfile;
            axisDispersionDepthHorizontal.Range = new Range<int>(0, threadData.n2LineLength);
            axisDispersionDepthVertical.Range = new Range<float>(UIData.fULIntensityMin, UIData.fULIntensityMax);

            UIData.pfDispersionSpectrum = new float[2, threadData.n1LineLength];
            graphDispersionSpectrum.DataSource = UIData.pfDispersionSpectrum;
            axisDispersionSpectrumHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            fMin = UIData.fULRawDataMin;
            fMax = UIData.fULRawDataMax;
            if (fMin >= fMax)
                fMax = fMin + 1.0f;
            axisDispersionSpectrumVertical.Range = new Range<float>(fMin, fMax);

            UIData.pfDispersionPhase = new float[3, threadData.n1LineLength];
            graphDispersionPhase.DataSource = UIData.pfDispersionPhase;
            axisDispersionPhaseHorizontal.Range = new Range<int>(0, threadData.n1LineLength);
            axisDispersionPhaseVertical.Range = new Range<float>(-20f, 20f);

            UIData.bDispersionLoad = false;
            UIData.bDispersionSave = false;
            UIData.bDispersionClear = false;

            #endregion dispersion tab

            #endregion LR

            #endregion  // set up graphs


            #region computation type

            int nDevice = UIData.nComputationDevice;

            threadData.nProcess1ProcessingType = 0;  // NI is default

            #if TRUEIPP
            if (nDevice == 1)
                threadData.nProcess1ProcessingType = 1;  // IPP
            #endif  // TRUEIPP

            #if TRUECUDA
            if (nDevice >= 2)
                threadData.nProcess1ProcessingType = 2;  // CUDA
            #endif  // TRUECUDA

            #endregion computation type


            #region timeouts

            threadData.nProcess1WriteTimeout = 500;  // milliseconds

            #endregion timeouts


            #region set up dlls

            // int ndllCUDA = nOCTcudaWrapper.initialize(UIData.nLLSystemType, threadData.nRawNumberAlines, threadData.nRawAlineLength, 256, threadData.nProcessedNumberAlines, threadData.nProcessedAlineLength);

            #endregion


            UIData.nConfigState = 1;

        }


        private void btnLRConfigurationStop_Click(object sender, RoutedEventArgs e)
        {
            UIData.nConfigState = 0;

            #region linked list
            CDataNode datanode;
            // clear linked list
            while (nodeList.Count > 0)
            {
                datanode = nodeList.Last();
                nodeList.RemoveLast();
                datanode = null;
            }   // while (nodeList.Count

            UIData.pnLinkedList = new int[2, 2];
            Array.Clear(UIData.pnLinkedList, 0, UIData.pnLinkedList.Length);
            graphLRDiagnostics.DataSource = UIData.pnLinkedList;

            GC.Collect();
            #endregion

            #region threads
            threadData.mreMainKill.Set();
            threadData.mreMainDead.WaitOne();
            #endregion

            #region dlls

            // int ndllCUDA = nOCTcudaWrapper.cleanup();
            #endregion
        }


        private void btnLROperationStart_Click(object sender, RoutedEventArgs e)
        {
#region threads
            threadData.mreMainRun.Set();
#endregion
        }


        private void btnLROperationStop_Click(object sender, RoutedEventArgs e)
        {
            threadData.mreMainKill.Set();
            threadData.mreMainDead.WaitOne();

        }   // private void btnLROperationStop_Click


        private void btnLRGCCollect_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }   // private void btnLRGCCollect_Click


        void MainThread()
        {
            #region initializing
            threadData.strMainThreadStatus = "Initializing...";

            // start other threads
            threadData.threadOutput = new Thread(OutputThread);
            threadData.threadOutput.Priority = ThreadPriority.Normal;
            threadData.threadOutput.Start();

            threadData.threadAcquire = new Thread(AcquireThread);
            threadData.threadAcquire.Priority = ThreadPriority.Normal;
            threadData.threadAcquire.Start();

            threadData.threadSave = new Thread(SaveThread);
            threadData.threadSave.Priority = ThreadPriority.Normal;
            threadData.threadSave.Start();

            threadData.threadProcess = new Thread(ProcessThread);
            threadData.threadProcess.Priority = ThreadPriority.Normal;
            threadData.threadProcess.Start();

            threadData.threadProcess1 = new Thread(Process1Thread);
            threadData.threadProcess1.Priority = ThreadPriority.Normal;
            threadData.threadProcess1.Start();

            threadData.threadProcess2 = new Thread(Process2Thread);
            threadData.threadProcess2.Priority = ThreadPriority.Normal;
            threadData.threadProcess2.Start();

            threadData.threadCleanup = new Thread(CleanupThread);
            threadData.threadCleanup.Priority = ThreadPriority.Normal;
            threadData.threadCleanup.Start();

            // wait for threads
            threadData.mreOutputReady.WaitOne();
            threadData.mreAcquireReady.WaitOne();
            threadData.mreSaveReady.WaitOne();
            threadData.mreProcessReady.WaitOne();
            threadData.mreProcess1Ready.WaitOne();
            threadData.mreProcess2Ready.WaitOne();
            threadData.mreCleanupReady.WaitOne();

            // set up wait handles for starting
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreMainKill;
            pweStart[1] = threadData.mreMainRun;
            // set up wait handles for main loop
            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreMainKill;
            pweLoop[1] = threadData.ssAcquireComplete.AvailableWaitHandle;

            // initialization complete
            threadData.mreMainReady.Set();
            threadData.strMainThreadStatus = "Ready!";
            #endregion

            #region main loop
            threadData.strMainThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.mreOutputRun.Set();
                threadData.mreAcquireRun.Set();
                threadData.mreSaveRun.Set();
                threadData.mreProcessRun.Set();
                threadData.mreProcess1Run.Set();
                threadData.mreProcess2Run.Set();
                threadData.mreCleanupRun.Set();

                threadData.strMainThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) != 0)
                {
                    threadData.strMainThreadStatus = "waiting";
                    threadData.ssAcquireComplete.Wait();
                    threadData.strMainThreadStatus = "updating";
                    threadData.mreCleanupAction.Set();
                    Thread.Sleep(1);
                }

            }
            #endregion

            #region cleanup
            threadData.strMainThreadStatus = "Cleaning up...";

            // send kill command to other threads
            threadData.mreAcquireKill.Set();
            threadData.mreAcquireDead.WaitOne();

            threadData.mreSaveKill.Set();
            threadData.mreSaveDead.WaitOne();

            threadData.mreProcessKill.Set();
            threadData.mreProcessDead.WaitOne();

            threadData.mreProcess1Kill.Set();
            threadData.mreProcess1Dead.WaitOne();

            threadData.mreProcess2Kill.Set();
            threadData.mreProcess2Dead.WaitOne();

            threadData.mreCleanupAction.Set();
            threadData.mreCleanupKill.Set();
            threadData.mreCleanupDead.WaitOne();

            // wait for threads to end
            threadData.mreOutputKill.Set();
            threadData.mreOutputDead.WaitOne();

            // all done
            threadData.mreMainDead.Set();
            threadData.strMainThreadStatus = "Done.";
            #endregion

        }


        void OutputThread()
        {
            #region initializing

            threadData.strOutputThreadStatus = "Initializing...";

            #region general variables
            // initialization
            double dLineTriggerRate = UIData.nLLLineRate;
            int nNumberLinesPerChunk = UIData.nLLLinesPerChunk;
            int nNumberChunksPerImage = UIData.nLLChunksPerImage;
            int nFrame, nNumberFrames = UIData.nLLImagesPerVolume;
            int nLine, nNumberLines = nNumberLinesPerChunk * nNumberChunksPerImage;
            int nWFM, nWFMCtrLength = 2 * (nNumberFrames * nNumberLines + 1), nWFMDigLength = 2 * (nNumberFrames * nNumberLines + 1), nWFMAnaLength = nNumberFrames * nNumberLines;
            int nIndex;

            // get mode
            int nMode = UIData.nLLSystemType;
            // 0 : SD-OCT
            // 1 : PS-OCT
            // 2 : line field
            // 3 : OFDI
            // 4 : PS-OFDI

            #endregion general variables

            #region counter task

            #if TRUEDAQ

            if (UIData.nOutputDigitalClockType == 0)
            {
                Task taskCtr = new Task();
                taskCtr.COChannels.CreatePulseChannelFrequency("Dev1/ctr0", "ctrClock", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0.0, 2 * dLineTriggerRate, 0.5);
                taskCtr.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples, nWFMCtrLength);
            }   // if (UIData

            #endif // TRUEDAQ

            #endregion counter task

            #region digital waveforms

            DigitalWaveform[] digWFM = new DigitalWaveform[3];

            #region line trigger
            nWFM = 0;
            digWFM[nWFM] = new DigitalWaveform(nWFMDigLength, 1);

            digWFM[nWFM].Signals[0].States[0] = DigitalState.ForceDown;
            digWFM[nWFM].Signals[0].States[1] = DigitalState.ForceDown;

            for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
            {
                for (nLine=0; nLine < nNumberLines; nLine++)
                {
                    nIndex = nFrame * nNumberLines + nLine + 2;
                    if (nLine % 2 == 0)
                        digWFM[nWFM].Signals[0].States[nIndex] = DigitalState.ForceUp;
                    else
                        digWFM[nWFM].Signals[0].States[nIndex] = DigitalState.ForceDown;
                }   // for (nLine
            }   // for (nFrame

            #endregion line trigger

            #region frame trigger
            nWFM = 1;
            digWFM[nWFM] = new DigitalWaveform(nWFMDigLength, 1);

            digWFM[nWFM].Signals[0].States[0] = DigitalState.ForceDown;
            digWFM[nWFM].Signals[0].States[1] = DigitalState.ForceDown;

            for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
            {
                for (nLine = 0; nLine < nNumberLines; nLine++)
                {
                    nIndex = nFrame * nNumberLines + nLine + 2;
                    if (nLine == 0)
                        digWFM[nWFM].Signals[0].States[nIndex] = DigitalState.ForceUp;
                    else
                        digWFM[nWFM].Signals[0].States[nIndex] = DigitalState.ForceDown;
                }   // for (nLine
            }   // for (nFrame

            #endregion frame trigger

            #region volume trigger
            nWFM = 2;
            digWFM[nWFM] = new DigitalWaveform(nWFMDigLength, 1);

            digWFM[nWFM].Signals[0].States[0] = DigitalState.ForceUp;
            digWFM[nWFM].Signals[0].States[1] = DigitalState.ForceDown;

            for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
            {
                for (nLine = 0; nLine < nNumberLines; nLine++)
                {
                    nIndex = nFrame * nNumberLines + nLine + 2;
                    digWFM[nWFM].Signals[0].States[nIndex] = DigitalState.ForceDown;
                }   // for (nLine
            }   // for (nFrame

            #endregion volume trigger


            #if TRUEDAQ

            Task taskDig = new Task();
            DigitalMultiChannelWriter digWriter = new DigitalMultiChannelWriter(taskDig.Stream);
            taskDig.DOChannels.CreateChannel("Dev1/port0/line0", "digLineTrigger", ChannelLineGrouping.OneChannelForEachLine);
            taskDig.DOChannels.CreateChannel("Dev1/port0/line1", "digFrameTrigger", ChannelLineGrouping.OneChannelForEachLine);
            taskDig.DOChannels.CreateChannel("Dev1/port0/line2", "digVolumeTrigger", ChannelLineGrouping.OneChannelForEachLine);
            if (UIData.nOutputDigitalClockType == 0)
            {
                taskDig.Timing.ConfigureSampleClock("/Dev1/Ctr0InternalOutput", 2 * dLineTriggerRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            }   // if (UIData
            else
            {
                // Axsun clock parameters needed here
                taskDig.Timing.ConfigureSampleClock("/Dev1/PFI9", 2 * dLineTriggerRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            }   // if (UIData
            taskDig.Control(TaskAction.Verify);

            digWriter.WriteWaveform(false, digWFM);

            #endif // TRUEDAQ

            #endregion digital waveforms

            #region analog waveforms

            float f = UIData.fLLCenterX;
            float[,] anaWFM = new float[3, nWFMAnaLength];

            #region fast and slow axes

            #region get variables from UI

            float fCenterX = UIData.fLLCenterX;
            float fCenterY = UIData.fLLCenterY;
            float fRangeFast = UIData.fLLRangeFast;
            float fRangeSlow = UIData.fLLRangeSlow;
            float fCosAngle = (float)Math.Cos((Math.PI / 180.0) * UIData.fLLFastAngle);
            float fSinAngle = (float)Math.Sin((Math.PI / 180.0) * UIData.fLLFastAngle);
            int nDwellFast = UIData.nLLDwellFast;
            int nDwellSlow = UIData.nLLDwellSlow;
            int nRound = UIData.nLLRounding;

            #endregion get variables from UI

            float fXSlope = fRangeFast / ((nNumberLines - nRound) / nDwellFast - 1);
            float fXOffset = -0.5f * fRangeFast;
            float fYSlope = fRangeSlow / (nNumberFrames / nDwellSlow - 1);
            float fYOffset = -0.5f * fRangeSlow;
            float fX, fY;

            #region initial WFM calculation

            for (nFrame=0; nFrame<nNumberFrames; nFrame++)
            {
                fY = fYSlope * (nFrame / nDwellSlow) + fYOffset;
                for (nLine=0; nLine < nNumberLines - nRound; nLine++)
                {
                    fX = fXSlope * (nLine / nDwellSlow) + fXOffset;

                    nIndex = nFrame * nNumberLines + nLine;
                    anaWFM[0, nIndex] = fCenterX + fX * fCosAngle - fY * fSinAngle;
                    anaWFM[1, nIndex] = fCenterY + fY * fCosAngle + fX * fSinAngle;
                }   // for (nLine
            }   // for (nFrame

            #endregion initial WFM calculation

            #region round edges

            float[] pfRound = new float[nRound];
            for (nLine = 0; nLine < nRound; nLine++)
                pfRound[nLine] = (float)(0.5 * (1.0 - Math.Cos(Math.PI * ((double)nLine) / ((double)nRound))));

            for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
            {
                fXOffset = anaWFM[0, (nFrame + 1) * nNumberLines - nRound - 1];
                fYOffset = anaWFM[1, (nFrame + 1) * nNumberLines - nRound - 1];
                if (nFrame == nNumberFrames - 1)
                {
                    fXSlope = anaWFM[0, 0] - fXOffset;
                    fYSlope = anaWFM[1, 0] - fYOffset;
                }
                else // if (nFrame
                {
                    fXSlope = anaWFM[0, (nFrame + 1) * nNumberLines] - fXOffset;
                    fYSlope = anaWFM[1, (nFrame + 1) * nNumberLines] - fYOffset;
                }   // if (nFrame
                for (nLine = nNumberLines - nRound; nLine < nNumberLines; nLine++)
                {
                    nIndex = nFrame * nNumberLines + nLine;
                    anaWFM[0, nIndex] = fXSlope * pfRound[nLine - (nNumberLines - nRound)] + fXOffset;
                    anaWFM[1, nIndex] = fYSlope * pfRound[nLine - (nNumberLines - nRound)] + fYOffset;
                }   // for (nLine
            }   // for (nFrame

            #endregion round edges

            #endregion fast and slow axes

            #region polarization modulator

            float fPolModLow = UIData.fLLPolModLow;
            float fPolModHigh = UIData.fLLPolModHigh;

            for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
                for (nLine = 0; nLine < nNumberLines; nLine++)
                    if (nLine % 2 == 0)
                        anaWFM[2, nFrame * nNumberLines + nLine] = fPolModLow;
                    else
                        anaWFM[2, nFrame * nNumberLines + nLine] = fPolModHigh;

            #endregion polarization modulator

            #if TRUEDAQ

            Task taskAna = new Task();
            AnalogMultiChannelWriter anaWriter = new AnalogMultiChannelWriter(taskAna.Stream);
            taskAna.AOChannels.CreateVoltageChannel("Dev1/ao0", "anaGalvoFast", -5.0, +5.0, AOVoltageUnits.Volts);
            taskAna.AOChannels.CreateVoltageChannel("Dev1/ao1", "anaGalvoSlow", -5.0, +5.0, AOVoltageUnits.Volts);
            taskAna.AOChannels.CreateVoltageChannel("Dev1/ao2", "anaPolMod", -5.0, +5.0, AOVoltageUnits.Volts);
            taskAna.Timing.ConfigureSampleClock("/Dev1/PFI7", dLineTriggerRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            anaWriter.WriteMultiSample(false, anaWFM);

            #endif // TRUEDAQ

            #endregion analog waveforms

            #region update graph

            Buffer.BlockCopy(anaWFM, 0, UIData.pfOutput, 0, anaWFM.Length * sizeof(float));
            
            #endregion update graph

            #region handles

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreOutputKill;
            pweStart[1] = threadData.mreOutputRun;
            // set up wait handles for main loop
            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreOutputKill;
            pweLoop[1] = threadData.mreOutputUpdate;

            #endregion handles

            // initialization complete
            threadData.mreOutputReady.Set();
            threadData.strOutputThreadStatus = "Ready!";

            #endregion initializing


            #region main loop

            threadData.strOutputThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strOutputThreadStatus = "GO!";

                #if (TRUEDAQ)
                // start tasks
                taskCtr.Start();
                taskDig.Control(TaskAction.Start);
                taskAna.Control(TaskAction.Start);
                #endif  // TRUEDAQ

                while (WaitHandle.WaitAny(pweLoop) == 1)
                {
                    threadData.mreOutputUpdate.Reset();
                    threadData.strOutputThreadStatus = "updating...";

                    #region fast and slow axes

                    #region get variables from UI

                    fCenterX = UIData.fLLCenterX;
                    fCenterY = UIData.fLLCenterY;
                    fRangeFast = UIData.fLLRangeFast;
                    fRangeSlow = UIData.fLLRangeSlow;
                    fCosAngle = (float)Math.Cos((Math.PI / 180.0) * UIData.fLLFastAngle);
                    fSinAngle = (float)Math.Sin((Math.PI / 180.0) * UIData.fLLFastAngle);
                    nDwellFast = UIData.nLLDwellFast;
                    nDwellSlow = UIData.nLLDwellSlow;
                    nRound = UIData.nLLRounding;

                    #endregion get variables from UI

                    fXSlope = fRangeFast / ((nNumberLines - nRound) / nDwellFast - 1);
                    fXOffset = -0.5f * fRangeFast;
                    fYSlope = fRangeSlow / (nNumberFrames / nDwellSlow - 1);
                    fYOffset = -0.5f * fRangeSlow;

                    #region initial WFM calculation

                    for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
                    {
                        fY = fYSlope * (nFrame / nDwellSlow) + fYOffset;
                        for (nLine = 0; nLine < nNumberLines - nRound; nLine++)
                        {
                            fX = fXSlope * (nLine / nDwellSlow) + fXOffset;

                            nIndex = nFrame * nNumberLines + nLine;
                            anaWFM[0, nIndex] = fCenterX + fX * fCosAngle - fY * fSinAngle;
                            anaWFM[1, nIndex] = fCenterY + fY * fCosAngle + fX * fSinAngle;
                        }   // for (nLine
                    }   // for (nFrame

                    #endregion initial WFM calculation

                    #region round edges

                    pfRound = new float[nRound];
                    for (nLine = 0; nLine < nRound; nLine++)
                        pfRound[nLine] = (float)(0.5 * (1.0 - Math.Cos(Math.PI * ((double)nLine) / ((double)nRound))));

                    for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
                    {
                        fXOffset = anaWFM[0, (nFrame + 1) * nNumberLines - nRound - 1];
                        fYOffset = anaWFM[1, (nFrame + 1) * nNumberLines - nRound - 1];
                        if (nFrame == nNumberFrames - 1)
                        {
                            fXSlope = anaWFM[0, 0] - fXOffset;
                            fYSlope = anaWFM[1, 0] - fYOffset;
                        }
                        else // if (nFrame
                        {
                            fXSlope = anaWFM[0, (nFrame + 1) * nNumberLines] - fXOffset;
                            fYSlope = anaWFM[1, (nFrame + 1) * nNumberLines] - fYOffset;
                        }   // if (nFrame
                        for (nLine = nNumberLines - nRound; nLine < nNumberLines; nLine++)
                        {
                            nIndex = nFrame * nNumberLines + nLine;
                            anaWFM[0, nIndex] = fXSlope * pfRound[nLine - (nNumberLines - nRound)] + fXOffset;
                            anaWFM[1, nIndex] = fYSlope * pfRound[nLine - (nNumberLines - nRound)] + fYOffset;
                        }   // for (nLine
                    }   // for (nFrame

                    #endregion round edges

                    #endregion fast and slow axes

                    #region polarization modulator

                    fPolModLow = UIData.fLLPolModLow;
                    fPolModHigh = UIData.fLLPolModHigh;

                    for (nFrame = 0; nFrame < nNumberFrames; nFrame++)
                        for (nLine = 0; nLine < nNumberLines; nLine++)
                            if (nLine % 2 == 0)
                                anaWFM[2, nFrame * nNumberLines + nLine] = fPolModLow;
                            else
                                anaWFM[2, nFrame * nNumberLines + nLine] = fPolModHigh;

                    #endregion polarization modulator

                    #if (TRUEDAQ)
                    anaWriter.BeginWriteMultiSample(false, anaWFM, null, null);
                    #endif

                    #region update graph

                    Buffer.BlockCopy(anaWFM, 0, UIData.pfOutput, 0, anaWFM.Length * sizeof(float));

                    #endregion update graph

                    threadData.strOutputThreadStatus = "idle...";
                }
            }

            #endregion main loop


            #region cleanup

            threadData.strOutputThreadStatus = "Cleaning up...";

            #if (TRUEDAQ)

            taskAna.Control(TaskAction.Stop);
            taskDig.Control(TaskAction.Stop);
            taskCtr.Stop();

            taskAna.Dispose();
            taskDig.Dispose();
            taskCtr.Dispose();

            #endif  // TRUEDAQ

            threadData.mreOutputDead.Set();
            threadData.strOutputThreadStatus = "Done.";

            #endregion cleanup

        }


        void AcquireThread()
        {
            #region initializing

            threadData.strAcquireThreadStatus = "init";

            // initialize sub-threads
            threadData.threadAcquireAlazar = new Thread(AcquireAlazarThread);
            threadData.threadAcquireAlazar.Priority = ThreadPriority.Normal;
            threadData.threadAcquireAlazar.Start();

            threadData.threadAcquireDAQ = new Thread(AcquireDAQThread);
            threadData.threadAcquireDAQ.Priority = ThreadPriority.Normal;
            threadData.threadAcquireDAQ.Start();

            threadData.threadAcquireIMAQ = new Thread(AcquireIMAQThread);
            threadData.threadAcquireIMAQ.Priority = ThreadPriority.Normal;
            threadData.threadAcquireIMAQ.Start();

            // wait for sub-threads to be ready
            threadData.mreAcquireAlazarReady.WaitOne();
            threadData.mreAcquireDAQReady.WaitOne();
            threadData.mreAcquireIMAQReady.WaitOne();

            // initialization
            bool bTroublemaker = false;
            threadData.nodeAcquire = nodeList.First;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreAcquireKill;
            pweStart[1] = threadData.mreAcquireRun;

            // wait handles to signal running
            WaitHandle[] pweGo = new WaitHandle[3];
            pweGo[0] = threadData.areAcquireAlazarGo;
            pweGo[1] = threadData.areAcquireDAQGo;
            pweGo[2] = threadData.areAcquireIMAQGo;

            // wait handles to signal completion
            WaitHandle[] pweComplete = new WaitHandle[3];
            pweComplete[0] = threadData.areAcquireAlazarComplete;
            pweComplete[1] = threadData.areAcquireDAQComplete;
            pweComplete[2] = threadData.areAcquireIMAQComplete;

            // initialization complete
            threadData.mreAcquireReady.Set();
            threadData.strAcquireThreadStatus = "Ready!";

            #endregion

            int nFileNumber = UIData.nLLFileNumber;  // initial value = 100001 
            int nFramePosition = 1;

            #region main loop

            threadData.strAcquireThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strAcquireThreadStatus = "GO!";
                threadData.mreAcquireAlazarRun.Set();
                threadData.mreAcquireDAQRun.Set();
                threadData.mreAcquireIMAQRun.Set();

                while (threadData.mreAcquireKill.WaitOne(0) == false)
                {
                    threadData.strAcquireThreadStatus = "acq";
                    if (threadData.nodeAcquire.Value.rwls.TryEnterWriteLock(1000) == true)
                    {
                        if (threadData.nodeAcquire.Value.nAcquired > 0)
                        {
                            // something wrong!
                            threadData.strAcquireThreadStatus = "overrun";
                            threadData.nodeAcquire.Value.rwls.ExitWriteLock();
                            bTroublemaker = true;
                            threadData.mreAcquireKill.Set();
                            threadData.mreMainKill.Set();
                        }
                        else
                        {  // if (nodeAcquire.Value.nAcquired

                            // actual acquisition
                            threadData.mreAcquireNodeReady.Set();
                            threadData.nodeAcquire.Value.bRecord = threadData.bRecord;

                            /* Begin: 20201208 editing by JL */                            
                            threadData.nodeAcquire.Value.strFilename = UIData.strLLFileDirectory + "//" + UIData.strLLFilePrefix + String.Format("{0}", nFileNumber) + ".dat";
                            threadData.nodeAcquire.Value.nFramePosition = nFramePosition;
                            threadData.nodeAcquire.Value.nFileNumber = nFileNumber;
                            threadData.nFramePosition = nFramePosition;
                            threadData.nFileNumber = nFileNumber;
                            nFileNumber++;
                            
                            nFramePosition++;

                            if (nFramePosition > UIData.nLLImagesPerVolume)
                                nFramePosition = 1; 
                            /* End: 20201208 editing by JL */


                            if (WaitHandle.WaitAll(pweGo, 10000) == true)
                            {
                                threadData.mreAcquireNodeReady.Reset();
                                if (WaitHandle.WaitAll(pweComplete, 10000) == true)
                                {
                                    // acquisition complete
                                    if (threadData.nodeAcquire.Value.bRecord)
                                        threadData.nodeAcquire.Value.nAcquired = 1;
                                    else
                                        threadData.nodeAcquire.Value.nAcquired = 2;
                                    threadData.nodeAcquire.Value.rwls.ExitWriteLock();
                                    // find next node
                                    threadData.nodeAcquire = threadData.nodeAcquire.Next;
                                    if (threadData.nodeAcquire == null)
                                        threadData.nodeAcquire = nodeList.First;
                                    // signal other threads
                                    threadData.ssAcquireComplete.Release();
                                    threadData.ssSaveAction.Release();
                                    threadData.ssProcessAction.Release();
                                    threadData.strAcquireThreadStatus = "done";
                                }
                                else
                                {  // if (WaitHandle.WaitAll(pweComplete
                                    // something wrong!
                                    threadData.strAcquireThreadStatus = "stuck";
                                    bTroublemaker = true;
                                    threadData.mreAcquireKill.Set();
                                    threadData.mreMainKill.Set();
                                }  // if (WaitHandle.WaitAll(pweComplete
                            }
                            else
                            {  // if (WaitHandle.WaitAll(pweGo
                                // something wrong!
                                threadData.strAcquireThreadStatus = "no go!";
                                bTroublemaker = true;
                                threadData.mreAcquireKill.Set();
                                threadData.mreMainKill.Set();
                            }  // if (WaitHandle.WaitAll(pweGo
                        }  // if (nodeAcquire.Value.nAcquired
                    }
                    else
                    {  // if (nodeAcquire.Value.rwls.TryEnterWriteLock
                        // something wrong!
                        threadData.strAcquireThreadStatus = "timeout!";
                        bTroublemaker = true;
                        threadData.mreAcquireKill.Set();
                        threadData.mreMainKill.Set();
                    }  // if (nodeAcquire.Value.rwls.TryEnterWriteLock
                }  // while (threadData.mreAcquireKill.WaitOne
            }  // if (WaitHandle.WaitAny

            #endregion

            #region cleanup

            if (bTroublemaker)
            {
                threadData.mreAcquireAlazarKill.Set();
                threadData.mreAcquireDAQKill.Set();
                threadData.mreAcquireIMAQKill.Set();
                threadData.mreAcquireAlazarDead.WaitOne();
                threadData.mreAcquireDAQDead.WaitOne();
                threadData.mreAcquireIMAQDead.WaitOne();

                threadData.mreAcquireDead.Set();
            }
            else
            {  // if (bTroublemaker
                #region cleanup
                threadData.strAcquireThreadStatus = "Cleaning up...";
                // clean up code
                threadData.mreAcquireAlazarKill.Set();
                threadData.mreAcquireDAQKill.Set();
                threadData.mreAcquireIMAQKill.Set();
                threadData.mreAcquireAlazarDead.WaitOne();
                threadData.mreAcquireDAQDead.WaitOne();
                threadData.mreAcquireIMAQDead.WaitOne();

                // signal other threads
                threadData.mreAcquireDead.Set();
                threadData.strAcquireThreadStatus = "Done.";
                #endregion
            }  // if (bTroublemaker
            #endregion

        }  // void AcquireThread


        void AcquireAlazarThread()
        {
#region initializing
            threadData.strAcquireAlazarThreadStatus = "i";

            // initialization
            bool bTroublemaker = false;
            int nMode = -1;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreAcquireAlazarKill;
            pweStart[1] = threadData.mreAcquireAlazarRun;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreAcquireAlazarKill;
            pweLoop[1] = threadData.mreAcquireNodeReady;
            int nStatus;

            // initialization complete
            switch (UIData.nLLSystemType)
            {
                case 0: // SD-OCT
                    nMode = 0;
                    break;
                case 1: // PS SD-OCT
                    nMode = 0;
                    break;
                case 2: // line field
                    nMode = 0;
                    break;
                case 3: // OFDI
                    nMode = 1;
                    break;
                case 4: // PS OFDI
                    nMode = 2;
                    break;
            }
            threadData.nSystemActual = UIData.nLLSystemActual;

            threadData.mreAcquireAlazarReady.Set();
            threadData.strAcquireAlazarThreadStatus = "r";
#endregion

#region main loop
            threadData.strAcquireAlazarThreadStatus = "s";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strAcquireAlazarThreadStatus = "g";
                while (threadData.mreAcquireAlazarKill.WaitOne(0) == false)
                {
                    nStatus = WaitHandle.WaitAny(pweLoop, 10000);
                    if (nStatus == 0)
                    {
                        // kill
                    }  // if (nStatus
                    if (nStatus == 1)
                    {
                        threadData.areAcquireAlazarGo.Set();
                        threadData.strAcquireAlazarThreadStatus = "G";
                        if (nMode > 0)
                        {
                            if (threadData.nSystemActual == 0)
                            {
                                threadData.strAcquireAlazarThreadStatus = "Wa";
                                ; // real acquisition
                                Thread.Sleep(1);
                            }
                            else
                            {
                                threadData.strAcquireAlazarThreadStatus = "Wd";
                                ; // read from file
                                Thread.Sleep(1);    // pgreg002 replace the sleep command with a block of code to read in a data file
                                                    // see section (marked with pgreg002) in the IMAQ acquisition thread for reference
                            }
                        }
                        threadData.areAcquireAlazarComplete.Set();
                        threadData.strAcquireAlazarThreadStatus = "D";
                    }  // if (nStatus
                    if (nStatus == WaitHandle.WaitTimeout)
                    {
                        threadData.strAcquireAlazarThreadStatus = "!";
                        bTroublemaker = true;
                        threadData.mreAcquireAlazarKill.Set();
                        threadData.mreMainKill.Set();
                    }  // if (nStatus
                }  // while (threadData.mreAcquireAlazarKill.WaitOne
            }  // if (WaitHandle.WaitAny
#endregion

#region cleanup
            if (bTroublemaker)
            {
                threadData.mreAcquireAlazarDead.Set();
            }
            else
            {  // if (bTroublemaker
#region cleanup
                threadData.strAcquireAlazarThreadStatus = "c";
                // clean up code
                ;
                // signal other threads
                threadData.mreAcquireAlazarDead.Set();
                threadData.strAcquireAlazarThreadStatus = "d";
#endregion
            }  // if (bTroublemaker
#endregion
        }


        void AcquireDAQThread()
        {
#region initializing
            threadData.strAcquireDAQThreadStatus = "i";

            // initialization
            bool bTroublemaker = false;
            int nMode = -1;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreAcquireDAQKill;
            pweStart[1] = threadData.mreAcquireDAQRun;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreAcquireDAQKill;
            pweLoop[1] = threadData.mreAcquireNodeReady;
            int nStatus;

            // initialization complete
            switch (UIData.nLLSystemType)
            {
                case 0: // SD-OCT
                    nMode = 1;
                    break;
                case 1: // PS SD-OCT
                    nMode = 1;
                    break;
                case 2: // line field
                    nMode = 1;
                    break;
                case 3: // OFDI
                    nMode = 1;
                    break;
                case 4: // PS OFDI
                    nMode = 1;
                    break;
            }
            threadData.mreAcquireDAQReady.Set();
            threadData.strAcquireDAQThreadStatus = "r";
#endregion

#region main loop
            threadData.strAcquireDAQThreadStatus = "s";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strAcquireDAQThreadStatus = "g";

                while (threadData.mreAcquireDAQKill.WaitOne(0) == false)
                {
                    nStatus = WaitHandle.WaitAny(pweLoop, 10000);
                    if (nStatus == 0)
                    {
                        ;
                    }
                    if (nStatus == 1)
                    {
                        threadData.areAcquireDAQGo.Set();
                        threadData.strAcquireDAQThreadStatus = "G";
                        if (nMode > 0)
                        {
                            if (threadData.nSystemActual == 0)
                            {
                                threadData.strAcquireDAQThreadStatus = "Wa";
                                ; // real acquisition
                                Thread.Sleep(1);
                            }
                            else
                            {
                                threadData.strAcquireDAQThreadStatus = "Wd";
                                ; // read from file
                                Thread.Sleep(1);
                            }
                        }
                        threadData.areAcquireDAQComplete.Set();
                        threadData.strAcquireDAQThreadStatus = "D";
                    }
                    if (nStatus == WaitHandle.WaitTimeout)
                    {
                        threadData.strAcquireDAQThreadStatus = "!";
                        bTroublemaker = true;
                        threadData.mreAcquireDAQKill.Set();
                        threadData.mreMainKill.Set();
                    }
                }  // while (threadData.mreAcquireDAQKill.WaitOne
            }  // if (WaitHandle.WaitAny
#endregion

#region cleanup
            if (bTroublemaker)
            {
                threadData.mreAcquireDAQDead.Set();
            }
            else
            {  // if (bTroublemaker
#region cleanup
                threadData.strAcquireDAQThreadStatus = "c";
                // clean up code
                ;
                // signal other threads
                threadData.mreAcquireDAQDead.Set();
                threadData.strAcquireDAQThreadStatus = "d";
#endregion
            }  // if (bTroublemaker
#endregion
        }


        void AcquireIMAQThread()
        {
            #region initializing
            threadData.strAcquireIMAQThreadStatus = "i";

            #region thread structures

            // initialization
            bool bTroublemaker = false;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreAcquireIMAQKill;
            pweStart[1] = threadData.mreAcquireIMAQRun;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreAcquireIMAQKill;
            pweLoop[1] = threadData.mreAcquireNodeReady;
            int nStatus;

            #endregion thread structures

            #region acquisition mode

            int nMode = -1;

            switch (UIData.nLLSystemType)
            {
                case 0: // SD-OCT
                    nMode = 1;
                    break;
                case 1: // PS SD-OCT
                    nMode = 2;
                    break;
                case 2: // line field
                    nMode = 1;
                    break;
                case 3: // OFDI
                    nMode = 0;
                    break;
                case 4: // PS OFDI
                    nMode = 0;
                    break;
            }

            #endregion acquisition mode

            #if TRUEIMAQ

            // InitializeIMAQ from dll
            string strinterfaceName0 = "img0";
            char[] pchinterfaceName0 = new char[64];
            pchinterfaceName0 = strinterfaceName0.ToCharArray();
            string strinterfaceName1 = "img1";
            char[] pchinterfaceName1 = new char[64];
            pchinterfaceName1 = strinterfaceName1.ToCharArray();
            int errInfo = 0;

            errInfo = nOCTimaqWrapper.InitializeImaq(pchinterfaceName0, pchinterfaceName1, UIData.nLLIMAQLineLength, UIData.nLLLinesPerChunk, errInfo);

            // since the initialization of imaq sometimes fails, there is while loop to make the initialiazation successed 
            while (errInfo < 0)
            {
                nOCTimaqWrapper.StopAcquisition();
                errInfo = 0;
                errInfo = nOCTimaqWrapper.InitializeImaq(pchinterfaceName0, pchinterfaceName1, UIData.nLLIMAQLineLength, UIData.nLLLinesPerChunk, errInfo);
            }

            if (errInfo < 0)
            {
                threadData.strAcquireIMAQThreadStatus = "F"; // status F meams Imaq Inialization failed
            }
            else
            {
                threadData.strAcquireIMAQThreadStatus = "r";
            }

            #endif  // TRUEIMAQ

            threadData.mreAcquireIMAQReady.Set();
            //threadData.strAcquireIMAQThreadStatus = "r";
            
            #endregion  // initialization

            #region main loop

            //threadData.strAcquireIMAQThreadStatus = "s";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strAcquireIMAQThreadStatus = "g";

                #if TRUEIMAQ

                // start acquisition call to dll
                nOCTimaqWrapper.StartAcquisition();

                #endif  // TRUEIMAQ

                while (threadData.mreAcquireIMAQKill.WaitOne(0) == false)
                {
                    nStatus = WaitHandle.WaitAny(pweLoop, 10000);
                    if (nStatus == 0)
                    {
                        ;
                    }
                    if (nStatus == 1)
                    {
                        threadData.areAcquireIMAQGo.Set();
                        threadData.strAcquireIMAQThreadStatus = "G";
                        if (nMode > 0)
                        {
                            if (threadData.nSystemActual == 0)
                            {
                                threadData.strAcquireIMAQThreadStatus = "Wa";

                                #if TRUEIMAQ

                                int bufferIndex0 = 0;   // this should be set outside of the loop?  bhp
                                int bufferIndex1 = 0;

                                for(int nChunk=0 ; nChunk<UIData.nLLChunksPerImage; nChunk++)
                                {
                                    nOCTimaqWrapper.RealAcquisition0(bufferIndex0, threadData.nodeAcquire.Value.pnIMAQParallel[nChunk]);
                                    nOCTimaqWrapper.RealAcquisition1(bufferIndex1, threadData.nodeAcquire.Value.pnIMAQPerpendicular[nChunk]);
                                }

                                #endif  // TRUEIMAQ

                            }   // if (threadData.nSystemActual
                            else
                            {
                                threadData.strAcquireIMAQThreadStatus = "Wd";
                                // read from file
                                var byteBuffer = new byte[UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk * UIData.nLLIMAQLineLength * sizeof(Int16)];
                                switch (threadData.nodeAcquire.Value.nNodeID % 5)
                                {
                                    case 0:
                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdH1.bin");
//                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Parallel_20201223_PSOCT_Calibration_102561.bin");
                                        break;
                                    case 1:
                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdH2.bin");
//                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Parallel_20201223_PSOCT_Calibration_102562.bin");
                                        break;
                                    case 2:
                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdH3.bin");
//                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Parallel_20201223_PSOCT_Calibration_102563.bin");
                                        break;
                                    case 3:
                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdH4.bin");
//                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Parallel_20201223_PSOCT_Calibration_102564.bin");
                                        break;
                                    case 4:
                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdH5.bin");
//                                        byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Parallel_20201223_PSOCT_Calibration_102565.bin");
                                        break;
                                }
                                if (UIData.nLLChunksPerImage > 0)
                                {
                                    for (int nChunk = 0; nChunk < UIData.nLLChunksPerImage; nChunk++)
                                        Buffer.BlockCopy(byteBuffer, nChunk * UIData.nLLLinesPerChunk * UIData.nLLIMAQLineLength * sizeof(Int16), threadData.nodeAcquire.Value.pnIMAQParallel[nChunk], 0, threadData.nodeAcquire.Value.pnIMAQParallel[nChunk].Length * sizeof(Int16));
                                    threadData.strAcquireIMAQThreadStatus = "Wd";
                                }   // if (nChunk

                                if (nMode > 1)
                                {
                                    switch (threadData.nodeAcquire.Value.nNodeID % 5)
                                    {
                                        case 0:
                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdV1.bin");
//                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Perpendicular_20201223_PSOCT_Calibration_102561.bin");
                                            break;
                                        case 1:
                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdV2.bin");
//                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Perpendicular_20201223_PSOCT_Calibration_102562.bin");
                                            break;
                                        case 2:
                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdV3.bin");
//                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Perpendicular_20201223_PSOCT_Calibration_102563.bin");
                                            break;
                                        case 3:
                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdV4.bin");
//                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Perpendicular_20201223_PSOCT_Calibration_102564.bin");
                                            break;
                                        case 4:
                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\image\\pdV5.bin");
//                                            byteBuffer = File.ReadAllBytes("C:\\Users\\hylep\\Desktop\\nOCT\\PSdata\\calibration\\Perpendicular_20201223_PSOCT_Calibration_102565.bin");
                                            break;
                                    }
                                    if (UIData.nLLChunksPerImage > 0)
                                    {
                                        for (int nChunk = 0; nChunk < UIData.nLLChunksPerImage; nChunk++)
                                            Buffer.BlockCopy(byteBuffer, nChunk * UIData.nLLLinesPerChunk * UIData.nLLIMAQLineLength * sizeof(Int16), threadData.nodeAcquire.Value.pnIMAQPerpendicular[nChunk], 0, threadData.nodeAcquire.Value.pnIMAQPerpendicular[nChunk].Length * sizeof(Int16));
                                    }   // if (nChunk
                                }   // if (nMode

                                Thread.Sleep((int)(1000 * UIData.nLLChunksPerImage * UIData.nLLLinesPerChunk / UIData.nLLLineRate));

                            }   // if (threadData.nSystemActual
                        }
                        threadData.areAcquireIMAQComplete.Set();
                        threadData.strAcquireIMAQThreadStatus = "D";
                    }
                    if (nStatus == WaitHandle.WaitTimeout)
                    {
                        threadData.strAcquireIMAQThreadStatus = "!";
                        bTroublemaker = true;
                        threadData.mreAcquireIMAQKill.Set();
                        threadData.mreMainKill.Set();
                    }
                }  // while (threadData.mreAcquireIMAQKill.WaitOne
            }  // if (WaitHandle.WaitAny

            #endregion  // main loop

            #region cleanup

            #if TRUEIMAQ

            // stopacquisition
            nOCTimaqWrapper.StopAcquisition();

            #endif  // TRUEIMAQ


            if (bTroublemaker)
            {
                threadData.mreAcquireIMAQDead.Set();
            }
            else
            {  // if (bTroublemaker
                #region cleanup
                threadData.strAcquireIMAQThreadStatus = "c";
                // clean up code
                ;
                // signal other threads
                threadData.mreAcquireIMAQDead.Set();
                threadData.strAcquireIMAQThreadStatus = "d";
                #endregion
            }  // if (bTroublemaker
            #endregion

            // call functon to stop cameras and clean ring buffers
        }


        void SaveThread()   // 20201208 editing 
        {
            #region initializing
            threadData.strSaveThreadStatus = "Initializing...";

            // initialization
            bool bTroublemaker = false;
            LinkedListNode<CDataNode> nodeSave;
            nodeSave = nodeList.First;

            /* Begin: 20201208 editing by JL */
            Thread.Sleep(1);
            string strTest;
            int nOffset1 = 4096;
            int nOffset2; 

            // parameters for saving 
            UInt16[][] pnAlazar;
            double[] pnDAQ;
            Int16[] pnIMAQ; 
            Int16[] pnIMAQParallel;
            Int16[] pnIMAQPerpendicular;            

            int nNumberChunks, nLinesPerChunk, nLineLength, nChannels;
            nNumberChunks = UIData.nLLChunksPerImage;
            nLinesPerChunk = UIData.nLLLinesPerChunk;
            nLineLength = UIData.nLLIMAQLineLength;
            int nNumberLines = nLinesPerChunk * nNumberChunks;

            pnDAQ = new double[4 * nNumberChunks * nLinesPerChunk];
            Array.Clear(pnDAQ, 0, pnDAQ.Length);

            // SD-OCT
            pnIMAQ = new Int16[nNumberLines * nLineLength];

            // PS-SD-OCT
            pnIMAQParallel = new Int16[nNumberLines * nLineLength];
            pnIMAQPerpendicular = new Int16[nNumberLines * nLineLength];

            switch (UIData.nLLSystemType)
            {
                case 0: // SD-OCT
                    Array.Clear(pnIMAQ, 0, pnIMAQ.Length);
                    break;
                case 1: // PS-SD-OCT
                    Array.Clear(pnIMAQParallel, 0, pnIMAQParallel.Length);
                    Array.Clear(pnIMAQPerpendicular, 0, pnIMAQParallel.Length);                    
                    break;
                case 2: // line field

                    break;
                case 3: // OFDI (pgreg002 here is a section to see how the arrays are defined in each node
                    nNumberChunks = UIData.nLLChunksPerImage;
                    nLinesPerChunk = UIData.nLLLinesPerChunk;
                    nLineLength = UIData.nLLAlazarLineLength;
                    nChannels = 0;
                    if (UIData.bLLAlazarCh1 == true)
                        nChannels++;
                    if (UIData.bLLAlazarCh2 == true)
                        nChannels++;
                    pnAlazar = new UInt16[nNumberChunks][];
                    for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                    {
                        pnAlazar[nChunk] = new UInt16[nChannels * nLinesPerChunk * nLineLength];  // 2 - MZI + OCT
                        // nSize += Convert.ToUInt64(nChannels * nLinesPerChunk * nLineLength) * sizeof(UInt16);
                        Array.Clear(pnAlazar[nChunk], 0, pnAlazar[nChunk].Length);
                    }   // for (int nChunk
                    pnDAQ = new double[4 * nNumberChunks * nLinesPerChunk];
                    // nSize += Convert.ToUInt64(4 * nNumberChunks * nLinesPerChunk * sizeof(double));
                    Array.Clear(pnDAQ, 0, pnDAQ.Length);
                    break;
                case 4: // PS-OFDI

                    break;
            }

            /* End: 20201208 editing by JL */

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreSaveKill;
            pweStart[1] = threadData.mreSaveRun;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.ssSaveAction.AvailableWaitHandle;
            pweLoop[1] = threadData.mreSaveKill;

            // initialization complete
            threadData.mreSaveReady.Set();
            threadData.strSaveThreadStatus = "Ready!";
            #endregion

            #region main loop
            threadData.strSaveThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strSaveThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) == 0)
                {
                    threadData.strSaveThreadStatus = "waiting (" + threadData.ssSaveAction.CurrentCount + ")!";
                    threadData.ssSaveAction.Wait();
                    threadData.strSaveThreadStatus = "saving (" + threadData.ssSaveAction.CurrentCount + ")!";
                    if (nodeSave.Value.rwls.TryEnterReadLock(1000) == true)
                    {
                        if (nodeSave.Value.nAcquired > 0)
                        {
                            /* Begin: 20201208 editing by JL */
                            threadData.nSaveNodeID = nodeSave.Value.nNodeID;                            
                            if (nodeSave.Value.bRecord)
                            {
                                // actual save
                                // Thread.Sleep(600);
                                FileStream fs = File.Open(nodeSave.Value.strFilename, FileMode.Create);
                                BinaryWriter binWriter = new BinaryWriter(fs);
                                strTest = nodeSave.Value.strFilename;   binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                switch (UIData.nLLSystemType)
                                {
                                    case 0: // SD-OCT
                                        strTest = "nFrameNumber=" + nodeSave.Value.nFramePosition + ";"; binWriter.Write(strTest.Length); binWriter.Write(strTest);
                                        strTest = "nNumberDataArrays=" + 2 + ";";       binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        // header array 1: IMAQ data (parallel and perpendicular)
                                        strTest = "strVar='pdIMAQ';";                   binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nOffset=" + nOffset1 + ";";          binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nNumberLines=" + nNumberLines + ";"; binWriter.Write(strTest.Length);    binWriter.Write
                                            (strTest);
                                        strTest = "nLineLength=" + nLineLength + ";";   binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "strDataType='int16';";               binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        // header array 2: DAQ data
                                        nOffset2 = nOffset1 + nNumberLines * nLineLength * sizeof(Int16);   // two cameras
                                        strTest = "strVar='pdDAQ';";                    binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nOffset=" + nOffset2 + ";";          binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nNumberLines=" + 4 + ";";            binWriter.Write(strTest.Length);    binWriter.Write(strTest);     // Need double check
                                        strTest = "nLineLength=" + nLineLength + ";";   binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "strDataType='double';";              binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        strTest = "END"; binWriter.Write(strTest.Length); binWriter.Write(strTest);

                                        // save array 1: IMAQ data (parallel and perpendicular)
                                        fs.Seek(nOffset1, SeekOrigin.Begin);

                                        for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                            Array.Copy(nodeSave.Value.pnIMAQ[nChunk], 0, pnIMAQ, nChunk * nLinesPerChunk * nLineLength, nLinesPerChunk * nLineLength);                                          

                                        for (int nLine = 0; nLine < nNumberLines; nLine++)
                                            for (int nPoint = 0; nPoint < nLineLength; nPoint++)
                                                binWriter.Write(pnIMAQ[nLine * nLineLength + nPoint]);

                                        break;
                                    case 1: // PS-SD-OCT
                                        strTest = "nFrameNumber=" + nodeSave.Value.nFramePosition + ";";    binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nNumberDataArrays=" + 2 + ";";       binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        // header array 1: IMAQ data (parallel and perpendicular)
                                        strTest = "strVar='pdIMAQx2';";                 binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nOffset=" + nOffset1 + ";";          binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nNumberLines=" + nNumberLines + ";"; binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nLineLength=" + nLineLength + ";";   binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "strDataType='int16';";               binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        // header array 2: DAQ data
                                        nOffset2 = nOffset1 + 2 * nNumberLines * nLineLength * sizeof(Int16);   // two cameras
                                        strTest = "strVar='pdDAQ';";                    binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nOffset=" + nOffset2 + ";";          binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "nNumberLines=" + 4 + ";";            binWriter.Write(strTest.Length);    binWriter.Write(strTest);     // Need double check
                                        strTest = "nLineLength=" + nLineLength + ";";   binWriter.Write(strTest.Length);    binWriter.Write(strTest);
                                        strTest = "strDataType='double';";              binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        strTest = "END"; binWriter.Write(strTest.Length);    binWriter.Write(strTest);

                                        // save array 1: IMAQ data (parallel and perpendicular)
                                        fs.Seek(nOffset1, SeekOrigin.Begin);

                                        for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                        {
                                            Array.Copy(nodeSave.Value.pnIMAQParallel[nChunk], 0, pnIMAQParallel, nChunk * nLinesPerChunk * nLineLength, nLinesPerChunk * nLineLength);
                                            Array.Copy(nodeSave.Value.pnIMAQPerpendicular[nChunk], 0, pnIMAQPerpendicular, nChunk * nLinesPerChunk * nLineLength, nLinesPerChunk * nLineLength);

                                        }   
                                        for(int nLine = 0; nLine < nNumberLines; nLine++)
                                        {
                                            for (int nPoint = 0; nPoint < nLineLength; nPoint++)
                                            {
                                                binWriter.Write(pnIMAQParallel[nLine * nLineLength + nPoint]);
                                                binWriter.Write(pnIMAQPerpendicular[nLine * nLineLength + nPoint]);
                                            }
                                        } 

                                        break;
                                    case 2: // line field

                                        break;
                                    case 3: // OFDI (pgreg002 here is a section to see how the arrays are defined in each node
                                        
                                        break;
                                    case 4: // PS-OFDI

                                        break;
                                } // switch (UIData.nLLSystemType)

                                fs.Close();                              

                                nodeSave.Value.nSaved = 1;

                                /* End: 20201208 editing by JL */
                            }
                            else
                            {   // if (nodeSave.Value.bRecord
                                nodeSave.Value.nSaved = 2;
                            }   // if (nodeSave.Value.bRecord
                            // save complete
                            nodeSave.Value.rwls.ExitReadLock();
                            // find next node
                            nodeSave = nodeSave.Next;
                            if (nodeSave == null)
                                nodeSave = nodeList.First;
                            // signal other threads
                            threadData.strSaveThreadStatus = "done (" + threadData.ssSaveAction.CurrentCount + ")!";
                        }
                        else
                        {  // if (nodeSave.Value.nAcquired
                            // something wrong
                            threadData.strSaveThreadStatus = "nothing to save! (" + threadData.ssSaveAction.CurrentCount + ")!";
                            nodeSave.Value.rwls.ExitReadLock();
                            bTroublemaker = true;
                            threadData.mreSaveKill.Set();
                            threadData.mreMainKill.Set();
                        }  // if (nodeSave.Value.nAcquired
                    }
                    else
                    {  // if (nodeSave.Value.rwls.TryEnterReadLock
                        // something wrong!
                        threadData.strSaveThreadStatus = "timeout! (" + threadData.ssSaveAction.CurrentCount + ")!";
                        bTroublemaker = true;
                        threadData.mreSaveKill.Set();
                        threadData.mreMainKill.Set();
                    }  // if (nodeSave.Value.rwls.TryEnterReadLock
                }  // while (WaitHandle.WaitAny
            }  // if (WaitHandle.WaitAny
            #endregion

#region cleanup
            if (bTroublemaker)
            {
                threadData.mreSaveDead.Set();
            }
            else
            {  // if (bTroublemaker
#region cleanup
                threadData.strSaveThreadStatus = "Cleaning up...";
                // clean up code
                ;
                // signal other threads
                threadData.mreSaveDead.Set();
                threadData.strSaveThreadStatus = "Done.";
#endregion
            }  // if (bTroublemaker
#endregion

        }   // void SaveThread


        void ProcessThread()
        {

            #region  initializing

            threadData.strProcessThreadStatus = "Initializing...";

            #region variables for traversing linked list

            bool bTroublemaker = false;
            LinkedListNode<CDataNode> nodeProcess;
            nodeProcess = nodeList.First;

            #endregion variables for traversing linked list

            #region variables for copying data from node to process1 structures

            int nChunk, nNumberChunks = UIData.nLLChunksPerImage;
            int nLine, nNumberLinesPerChunk = UIData.nLLLinesPerChunk;
            int nPoint, nLineLength = threadData.n1LineLength, nLineSize = nLineLength * sizeof(float);

            int nSection;
            float[] pfLine = new float[threadData.n1LineLength];
            int nSumLines;
            float[] pfSum = new float[threadData.n1LineLength];
            switch (UIData.nLLSystemType)
            {
                case 0:  // SD-OCT
                    break;
                case 1:  // PS-OCT
                    break;
                case 2:  // line field
                    break;
                case 3:  // OFDI
                    break;
            }

            #endregion variables for copying data from node to process1 structures

            #region wait handles for loops

            // set up wait handles to start main loop
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreProcessKill;
            pweStart[1] = threadData.mreProcessRun;

            // set up wait handles for main loop
            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreProcessKill;
            pweLoop[1] = threadData.ssProcessAction.AvailableWaitHandle;

            #endregion wait handles for loops

            // initialization complete
            threadData.mreProcessReady.Set();
            threadData.strProcessThreadStatus = "Ready!";

            #endregion  initializing

            #region main loop

            threadData.strProcessThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strProcessThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) != 0)
                {

                    #region search for most recent acquired node (based on value of ssProcessAction, mark others along the way

                    threadData.strProcessThreadStatus = "finding node (" + threadData.ssProcessAction.CurrentCount + ")!";
                    while (threadData.ssProcessAction.CurrentCount > 1)
                    {
                        threadData.ssProcessAction.Wait();
                        if (nodeProcess.Value.rwls.TryEnterReadLock(0) == true)
                        {
                            nodeProcess.Value.nProcessed = 2;
                            nodeProcess.Value.rwls.ExitReadLock();
                            nodeProcess = nodeProcess.Next;
                            if (nodeProcess == null)
                                nodeProcess = nodeList.First;
                        }
                        else  // if (nodeProcess.Value.rwls.TryEnterReadLock
                        {
                            // something wrong
                            threadData.strProcessThreadStatus = "problem finding node! (" + threadData.ssProcessAction.CurrentCount + ")!";
                            bTroublemaker = true;
                            threadData.mreProcessKill.Set();
                            threadData.mreMainKill.Set();
                        }  // if (nodeProcess.Value.rwls.TryEnterReadLock
                    }  // while (threadData.ssProcessAction.CurrentCount

                    #endregion  search for most recent acquired node, mark others along the way

                    #region work on most recent acquired node

                    threadData.strProcessThreadStatus = "at correct node (" + threadData.ssProcessAction.CurrentCount + ")!";
                    threadData.ssProcessAction.Wait();
                    if (nodeProcess.Value.rwls.TryEnterReadLock(1) == true)
                    {
                        threadData.strProcessThreadStatus = "processing (" + threadData.ssProcessAction.CurrentCount + ")!";
                        if (threadData.rwls1.TryEnterWriteLock(1) == true)
                        {
                            threadData.nProcessNode = nodeProcess.Value.nNodeID;
                            threadData.strProcessThreadStatus = "processing (" + threadData.ssProcessAction.CurrentCount + ")!";

                            #region copy data arrays in node to process1 buffers

                            switch (UIData.nLLSystemType)
                            {
                                case 0: // SD-OCT
                                    threadData.strProcessThreadStatus = "processingXXX (" + threadData.ssProcessAction.CurrentCount + ")!";
                                    threadData.strProcessThreadStatus = "processingYYY (" + nodeProcess.Value.nNodeID + ")!";

                                    #region convert from int to float while extracting calibration line

                                    nSection = 0;
                                    nSumLines = 0;
                                    Array.Clear(pfSum, 0, threadData.n1LineLength);
                                    for (nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                    {
                                        for (nLine = 0; nLine < nNumberLinesPerChunk; nLine++)
                                        {
                                            Array.Copy(nodeProcess.Value.pnIMAQParallel[nChunk], nLine * nLineLength, pfLine, 0, nLineLength);  // int to float while copying to pfLine
                                            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();  // update sum line
                                            Buffer.BlockCopy(pfLine, 0, threadData.pf1Interference, (nSection * threadData.n1NumberInterferenceLinesPerCalibration + nSumLines) * nLineLength * sizeof(float), nLineLength * sizeof(float));  // copy pfLine to interference buffer
                                            nSumLines++;  // update number of lines
                                        }   // for (nAline
                                    }   // for (nChunk
                                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                        threadData.pf1Calibration[nSection * nLineLength + nPoint] = pfSum[nPoint] / ((float)nSumLines);  // copy to calibration line while dividing sum by number

                                    #endregion convert from int to float while extracting calibration line

                                    #region copy DAQ data

//                                    Buffer.BlockCopy(nodeProcess.Value.pfDAQ, 0, threadData.pf1DAQ, 0, nodeProcess.Value.pfDAQ.Length);

                                    #endregion  copy DAQ data

                                    break;
                                case 1: // PS SD-OCT
                                    threadData.strProcessThreadStatus = "processingXXX (" + threadData.ssProcessAction.CurrentCount + ")!";
                                    threadData.strProcessThreadStatus = "processingYYY (" + nodeProcess.Value.nNodeID + ")!";

                                    #region parallel even: convert from int to float while extracting calibration line
                                    nSection = 0;
                                    nSumLines = 0;
                                    Array.Clear(pfSum, 0, threadData.n1LineLength);
                                    for (nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                    {
                                        for (nLine = 0; nLine < nNumberLinesPerChunk; nLine += 2)
                                        {
                                            Array.Copy(nodeProcess.Value.pnIMAQParallel[nChunk], nLine * nLineLength, pfLine, 0, nLineLength);  // int to float while copying to pfLine
                                            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();  // update sum line
                                            Buffer.BlockCopy(pfLine, 0, threadData.pf1Interference, (nSection * threadData.n1NumberInterferenceLinesPerCalibration + nSumLines) * nLineLength * sizeof(float), nLineLength * sizeof(float));  // copy pfLine to interference buffer
                                            nSumLines++;  // update number of lines
                                        }   // for (nAline
                                    }   // for (nChunk
                                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                        threadData.pf1Calibration[nSection * nLineLength + nPoint] = pfSum[nPoint] / ((float)nSumLines);  // copy to calibration line while dividing sum by number
                                    #endregion parallel even: convert from int to float while extracting calibration line

                                    #region parallel odd: convert from int to float while extracting calibration line
                                    nSection = 1;
                                    nSumLines = 0;
                                    Array.Clear(pfSum, 0, threadData.n1LineLength);
                                    for (nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                    {
                                        for (nLine = 1; nLine < nNumberLinesPerChunk; nLine += 2)
                                        {
                                            Array.Copy(nodeProcess.Value.pnIMAQParallel[nChunk], nLine * nLineLength, pfLine, 0, nLineLength);  // int to float while copying to pfLine
                                            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();  // update sum line
                                            Buffer.BlockCopy(pfLine, 0, threadData.pf1Interference, (nSection * threadData.n1NumberInterferenceLinesPerCalibration + nSumLines) * nLineLength * sizeof(float), nLineLength * sizeof(float));  // copy pfLine to interference buffer
                                            nSumLines++;  // update number of lines
                                        }   // for (nAline
                                    }   // for (nChunk
                                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                        threadData.pf1Calibration[nSection * nLineLength + nPoint] = pfSum[nPoint] / ((float)nSumLines);  // copy to calibration line while dividing sum by number
                                    #endregion parallel odd: convert from int to float while extracting calibration line

                                    #region perpendicular even: convert from int to float while extracting calibration line
                                    nSection = 2;
                                    nSumLines = 0;
                                    Array.Clear(pfSum, 0, threadData.n1LineLength);
                                    for (nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                    {
                                        for (nLine = 0; nLine < nNumberLinesPerChunk; nLine += 2)
                                        {
                                            for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                                pfLine[nLineLength - 1 - nPoint] = nodeProcess.Value.pnIMAQPerpendicular[nChunk][nLine * nLineLength + nPoint];
                                            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();  // update sum line
                                            Buffer.BlockCopy(pfLine, 0, threadData.pf1Interference, (nSection * threadData.n1NumberInterferenceLinesPerCalibration + nSumLines) * nLineLength * sizeof(float), nLineLength * sizeof(float));  // copy pfLine to interference buffer
                                            nSumLines++;  // update number of lines
                                        }   // for (nAline
                                    }   // for (nChunk
                                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                        threadData.pf1Calibration[nSection * nLineLength + nPoint] = pfSum[nPoint] / ((float)nSumLines);  // copy to calibration line while dividing sum by number
                                    #endregion perpendicular even: convert from int to float while extracting calibration line

                                    #region perpendicular odd: convert from int to float while extracting calibration line
                                    nSection = 3;
                                    nSumLines = 0;
                                    Array.Clear(pfSum, 0, threadData.n1LineLength);
                                    for (nChunk = 0; nChunk < nNumberChunks; nChunk++)
                                    {
                                        for (nLine = 1; nLine < nNumberLinesPerChunk; nLine += 2)
                                        {
                                            for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                                pfLine[nLineLength - 1 - nPoint] = nodeProcess.Value.pnIMAQPerpendicular[nChunk][nLine * nLineLength + nPoint];
                                            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();  // update sum line
                                            Buffer.BlockCopy(pfLine, 0, threadData.pf1Interference, (nSection * threadData.n1NumberInterferenceLinesPerCalibration + nSumLines) * nLineLength * sizeof(float), nLineLength * sizeof(float));  // copy pfLine to interference buffer
                                            nSumLines++;  // update number of lines
                                        }   // for (nAline
                                    }   // for (nChunk
                                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                                        threadData.pf1Calibration[nSection * nLineLength + nPoint] = pfSum[nPoint] / ((float)nSumLines);  // copy to calibration line while dividing sum by number
                                    #endregion perpendicular odd: convert from int to float while extracting calibration line

                                    #region copy DAQ data

//                                    Buffer.BlockCopy(nodeProcess.Value.pfDAQ, 0, threadData.pfProcess1DAQ, 0, nodeProcess.Value.pfDAQ.Length);

                                    #endregion copy DAQ data

                                    break;
                                case 2: // line field

                                    break;
                                case 3: // OFDI

                                    break;
                                case 4: // PS OFDI

                                    break;
                            }   // switch (UIData.nLLSystemType

                            #endregion  copy data arrays from node to process1 buffers

                            #region mark and release node

                            nodeProcess.Value.nProcessed = 1;
                            nodeProcess.Value.rwls.ExitReadLock();

                            #endregion mark and release node

                            #region release process1 write lock and signal process1 thread

                            threadData.rwls1.ExitWriteLock();
                            threadData.mreProcess1Action.Set();

                            #endregion release process1 write lock and signal process1 thread

                            #region copy from process1 buffers to spectrum plots

                            if (threadData.rwls1.TryEnterReadLock(threadData.nProcess1WriteTimeout) == true)
                            {

                                if (UIData.nULDisplayIndex == 0)  // raw data
                                {

                                    #region main UL image

                                    Buffer.BlockCopy(threadData.pf1Interference, 0, UIData.pfULImage, 0, threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * threadData.n1LineLength * sizeof(float));

                                    #endregion main UL image

                                    #region left

                                    nLine = UIData.nULLeft;
                                    if (nLine < 0)
                                        nLine = 0;
                                    if (nLine >= threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration)
                                        nLine = threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration - 1;

                                    for (nPoint = 0; nPoint < threadData.n1LineLength; nPoint++)
                                        UIData.pfULLeft[0, nPoint] = UIData.pfULImage[nLine, nPoint];

                                    // XXX add in DAQ data

                                    #endregion left

                                    #region top

                                    nPoint = UIData.nULTop;
                                    if (nPoint < 0)
                                        nPoint = 0;
                                    if (nPoint >= threadData.n1LineLength)
                                        nPoint = threadData.n1LineLength - 1;

                                    for (nLine = 0; nLine < threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration; nLine++)
                                        UIData.pfULTop[0, nLine] = UIData.pfULImage[nLine, nPoint];

                                    // XXX add in calibration data

                                    #endregion top

                                }   // if (UIData.nULDisplayIndex

                                threadData.rwls1.ExitReadLock();

                            }   // if (threadData.rwlsProcessTo1.TryEnterReadLock

                            #endregion  // copy from process1 buffers to spectrum plots

                        }
                        else  // if (threadData.rwlsProcessTo1.TryEnterWriteLock
                        {

                            #region mark and release node

                            nodeProcess.Value.nProcessed = 2;
                            nodeProcess.Value.rwls.ExitReadLock();

                            #endregion mark and release node

                        }    // if (threadData.rwlsProcessTo1.TryEnterWriteLock

                        nodeProcess = nodeProcess.Next;
                        if (nodeProcess == null)
                            nodeProcess = nodeList.First;
                    }
                    else  // if (nodeProcess.Value.rwls.TryEnterReadLock
                    {
                        // something wrong
                        threadData.strProcessThreadStatus = "problem processing node! (" + threadData.ssProcessAction.CurrentCount + ")!";
                        bTroublemaker = true;
                        threadData.mreProcessKill.Set();
                        threadData.mreMainKill.Set();
                    }  // if (nodeProcess.Value.rwls.TryEnterReadLock

                    #endregion work on most recent acquired node

                }  // while (WaitHandle.WaitAny

            }  // if (WaitHandle.WaitAny

            #endregion  // main loop

            #region cleanup

            if (bTroublemaker)
            {
                threadData.mreProcessDead.Set();
            }
            else  // if (bTroublemaker
            {
                threadData.strProcessThreadStatus = "Cleaning up...";
                // clean up code
                ;
                // signal other threads
                threadData.mreProcessDead.Set();
                threadData.strProcessThreadStatus = "Done.";
            }  // if (bTroublemaker

            #endregion  cleanup

        }


        void calculateCalibrationOld(int nNumberLines, float[] pfData, float[] pfMask, ref float[] pfDepthProfile, int nLeft, int nRight, ref float[] pfSpectrum, ref float[] pfPhase, ref float[] pfK, ref int[] pnIndex)
        {
            int nLine, nPoint, nLineLength = pfMask.Length, nTemp, nLast;
            float fTemp;
            double dTemp, dLeftSum = 0.0, dRightSum = 0.0, dSlope, dOffset;
            double[] pdLine = new double[nLineLength];
            double[] pdFitLine = new double[nLineLength];
            int[] pnAssigned = new int[nLineLength];
            ComplexDouble[] pcdFFT = new ComplexDouble[nLineLength];
            ComplexDouble[] pcdSpectrum = new ComplexDouble[nLineLength];

            for (nLine = 0; nLine < nNumberLines; nLine++)
            {
                #region copy and convert one line from float to double (required by NI's FFT)
                Array.Copy(pfData, nLine * nLineLength, pdLine, 0, nLineLength);
                #endregion  // copy and convert one line from float to double (required by NI's FFT)

                #region forward fft
                pcdFFT = NationalInstruments.Analysis.Dsp.Transforms.RealFft(pdLine);
                #endregion forward fft
                #region multiply mask and copy depth profile graph lines
                for (nPoint=0; nPoint<nLineLength >> 1; nPoint++)
                {
                    pfDepthProfile[(2 * nLine + 0) * (nLineLength >> 1) + nPoint] = (float) (20.0*Math.Log10(pcdFFT[nPoint].Magnitude));
                    pcdFFT[nPoint].Real = pcdFFT[nPoint].Real * pfMask[nPoint];
                    pcdFFT[nPoint].Imaginary = pcdFFT[nPoint].Imaginary * pfMask[nPoint];
                    pfDepthProfile[(2 * nLine + 1) * (nLineLength >> 1) + nPoint] = (float)(20.0 * Math.Log10(pcdFFT[nPoint].Magnitude));
                }   // for (nPoint
                for (nPoint = nLineLength >> 1; nPoint < nLineLength; nPoint++)
                {
                    pcdFFT[nPoint].Real = pcdFFT[nPoint].Real * pfMask[nPoint];
                    pcdFFT[nPoint].Imaginary = pcdFFT[nPoint].Imaginary * pfMask[nPoint];
                }   // for (nPoint
                #endregion multiply mask and copy depth profile graph lines

                #region inverse fft
                pcdSpectrum = NationalInstruments.Analysis.Dsp.Transforms.InverseFft(pcdFFT, false);
                #endregion inverse fft

                #region copy spectrum graph lines while calculating phase
                for (nPoint = 0; nPoint < nLeft; nPoint++)
                {
                    pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                    fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                    pfSpectrum[(2 * nLine + 0) * nLineLength + nPoint] = fTemp;
                    pfSpectrum[(2 * nLine + 1) * nLineLength + nPoint] = Single.NaN;
                }
                for (nPoint = nLeft; nPoint < nRight; nPoint++)
                {
                    pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                    fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                    pfSpectrum[(2 * nLine + 0) * nLineLength + nPoint] = fTemp;
                    pfSpectrum[(2 * nLine + 1) * nLineLength + nPoint] = fTemp;
                }
                for (nPoint = nRight; nPoint < nLineLength; nPoint++)
                {
                    pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                    fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                    pfSpectrum[(2 * nLine + 0) * nLineLength + nPoint] = fTemp;
                    pfSpectrum[(2 * nLine + 1) * nLineLength + nPoint] = Single.NaN;
                }
                #endregion copy spectrum graph lines while calculating phase
                #region unwrap phase
                NationalInstruments.Analysis.Dsp.SignalProcessing.UnwrapPhase(pdLine);
                #endregion unwrap phase
                #region bring midpoint down near 0 phase
                nPoint = (nLeft + nRight) >> 1;
                dTemp = (2.0 * Math.PI) * Math.Floor(pdLine[nPoint] / (2.0 * Math.PI) + 0.5);
                for (nPoint = 0; nPoint < nLineLength; nPoint++)
                    pdLine[nPoint] -= dTemp;
                #endregion bring midpoint down near 0 phase
                #region clip end points
                dLeftSum += pdLine[nLeft];
                dRightSum += pdLine[nRight];
                #endregion clip end points
                #region copy to phase plot lines
                for (nPoint=0; nPoint < nLineLength; nPoint++)
                    pfPhase[nLine * nLineLength + nPoint] = (float)(pdLine[nPoint]);
                #endregion copy to phase plot lines
            }   // for (nLine

            #region calculate overall slope and offset
            dLeftSum /= nNumberLines;
            dRightSum /= nNumberLines;
            dSlope = (dRightSum - dLeftSum) / (nRight - nLeft);
            dOffset = dLeftSum - dSlope * nLeft;
            #endregion
            #region calculate fit line and copy to graph line
            for (nPoint = 0; nPoint < nLineLength; nPoint++) {
                pdFitLine[nPoint] = dSlope * nPoint + dOffset;
                pfPhase[nNumberLines * nLineLength + nPoint] = (float) pdFitLine[nPoint];
            }   // for (nPoint
            #endregion

            for (nLine = 0; nLine < nNumberLines; nLine++)
            {
                #region initial k assignment
                Array.Clear(pnAssigned, 0, nLineLength);
                for (nPoint=0; nPoint < nLineLength; nPoint++)
                {
                    dTemp = (pfPhase[nLine * nLineLength + nPoint] - dOffset) / dSlope;
                    pfK[nLine * nLineLength + nPoint] = (float)dTemp;
                    nTemp = (int)Math.Ceiling(dTemp);
                    if ((0 <= nTemp) && (nTemp < nLineLength))
                    {
                        pnIndex[nLine * nLineLength + nTemp] = nPoint - 1;
                        pnAssigned[nTemp] = 1;
                    }   // if ((0
                }   // for (nPoint
                #endregion initial k assignment

                #region examine any initial unassigned points
                nPoint = 0;
                nTemp = 0;
                while ((pnAssigned[nPoint] == 0) && (nPoint < nLineLength-1))
                {
                    while ((nPoint >= pfK[nLine * nLineLength + nTemp + 1]) && (nTemp < nLineLength - 1))
                        nTemp++;
                    pnIndex[nLine * nLineLength + nPoint] = nTemp;
                    nPoint++;
                }   // while (pnAssigned[nPoint]
                #endregion examine any initial unassigned points

                if (nPoint < nLineLength)
                {
                    #region fill in all other unassigned points
                    nTemp = nPoint;
                    nLast = pnIndex[nLine * nLineLength + nPoint];
                    if (nLast < 0)
                        nLast = 0;
                    if (nLast > nLineLength - 4)
                        nLast = nLineLength - 4;
                    pnIndex[nLine * nLineLength + nPoint] = nLast;
                    for (nPoint = nTemp + 1; nPoint < nLineLength; nPoint++)
                        if (pnAssigned[nPoint] == 1)
                        {
                            nLast = pnIndex[nLine * nLineLength + nPoint];
                            if (nLast < 0)
                                nLast = 0;
                            if (nLast > nLineLength - 4)
                                nLast = nLineLength - 4;
                            pnIndex[nLine * nLineLength + nPoint] = nLast;
                        }
                        else
                            pnIndex[nLine * nLineLength + nPoint] = nLast;
                    #endregion fill in all other unassigned points
                }
                else  // if (nPoint
                {
                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                        pnIndex[nLine * nLineLength + nPoint] = nPoint;
                }

            }   // for (nLine

        }   // void calculateCalibration


        void clearCalibration(int nNumberLines, ref float[] pfK, ref int[] pnIndex)
        {
            int nLine, nPoint, nLineLength = pfK.Length / nNumberLines;
            int nIndex;
            for (nLine=0; nLine<nNumberLines; nLine++)
            {
                for (nPoint=0; nPoint<nLineLength; nPoint++)
                {
                    nIndex = nPoint - 2;
                    if (nIndex < 0)
                        nIndex = 0;
                    if (nIndex > nLineLength - 4)
                        nIndex = nLineLength - 4;

                    pfK[nLine * nLineLength + nPoint] = nPoint;
                    pnIndex[nLine * nLineLength + nPoint] = nIndex;
                }   // for (nPoint
            }   // for (nLine
        }   // void clearCalibration


        void applyCalibration(int nNumberCalibrationLines, int nNumberOCTLinesPerCalibration, ref float[] pfOCT, float[] pfK, int[] pnIndex)
        {
            int nCalibrationLine, nLine, nPoint, nLineLength = pfK.Length / nNumberCalibrationLines;
            int nIndex, nLineOffset;
            float[] pfLine = new float[nLineLength];
            float[] pfKLine = new float[nLineLength];
            int[] pnIndexLine = new int[nLineLength];
            float fx1_3, fx1_2, fx1, fy1;
            float fx2_3, fx2_2, fx2, fy2;
            float fx3_3, fx3_2, fx3, fy3;
            float fx4_3, fx4_2, fx4, fy4;

            float f4, f0;

            for (nCalibrationLine = 0; nCalibrationLine < nNumberCalibrationLines; nCalibrationLine++)
            {
                Buffer.BlockCopy(pfK, nCalibrationLine * nLineLength * sizeof(float), pfKLine, 0, nLineLength * sizeof(float));
                Buffer.BlockCopy(pnIndex, nCalibrationLine * nLineLength * sizeof(int), pnIndexLine, 0, nLineLength * sizeof(float));
                for (nLine = 0; nLine < nNumberOCTLinesPerCalibration; nLine++)
                {
                    nLineOffset = (nCalibrationLine * nNumberOCTLinesPerCalibration + nLine) * nLineLength;
                    Buffer.BlockCopy(pfOCT, nLineOffset * sizeof(float), pfLine, 0, nLineLength * sizeof(float));
                    for (nPoint = 0; nPoint < nLineLength; nPoint++)
                    {
                        nIndex = pnIndexLine[nPoint];
                        if (nIndex < 0)
                            nIndex = 0;
                        if (nIndex > nLineLength - 4)
                            nIndex = nLineLength - 4;

                        #region calculate cubic spline using Cramer's rule
                        fy1 = pfLine[nIndex + 0]; fx1 = (pfKLine[nIndex + 0] - nPoint); fx1_2 = fx1 * fx1; fx1_3 = fx1_2 * fx1;
                        fy2 = pfLine[nIndex + 1]; fx2 = (pfKLine[nIndex + 1] - nPoint); fx2_2 = fx2 * fx2; fx2_3 = fx2_2 * fx2;
                        fy3 = pfLine[nIndex + 2]; fx3 = (pfKLine[nIndex + 2] - nPoint); fx3_2 = fx3 * fx3; fx3_3 = fx3_2 * fx3;
                        fy4 = pfLine[nIndex + 3]; fx4 = (pfKLine[nIndex + 3] - nPoint); fx4_2 = fx4 * fx4; fx4_3 = fx4_2 * fx4;
                        //fy1 = pfLine[nIndex + 0]; fx1 = (pfKLine[nIndex + 0] - (nLineLength >> 1)); fx1_2 = fx1 * fx1; fx1_3 = fx1_2 * fx1;
                        //fy2 = pfLine[nIndex + 1]; fx2 = (pfKLine[nIndex + 1] - (nLineLength >> 1)); fx2_2 = fx2 * fx2; fx2_3 = fx2_2 * fx2;
                        //fy3 = pfLine[nIndex + 2]; fx3 = (pfKLine[nIndex + 2] - (nLineLength >> 1)); fx3_2 = fx3 * fx3; fx3_3 = fx3_2 * fx3;
                        //fy4 = pfLine[nIndex + 3]; fx4 = (pfKLine[nIndex + 3] - (nLineLength >> 1)); fx4_2 = fx4 * fx4; fx4_3 = fx4_2 * fx4;

                        //if ((fx2 <= nPoint) && (nPoint <= fx3))
                        //    ;
                        //else
                        //    if ((100 < nPoint) && (nPoint < 800))
                        //        f0 = 1;

                        //f1 = fy1 * ((fx2_2 * fx3 + fx3_2 * fx4 + fx4_2 * fx2) - (fx2_2 * fx4 + fx3_2 * fx2 + fx4_2 * fx3))
                        //   - fy2 * ((fx1_2 * fx3 + fx3_2 * fx4 + fx4_2 * fx1) - (fx1_2 * fx4 + fx3_2 * fx1 + fx4_2 * fx3))
                        //   + fy3 * ((fx1_2 * fx2 + fx2_2 * fx4 + fx4_2 * fx1) - (fx1_2 * fx4 + fx2_2 * fx1 + fx4_2 * fx2))
                        //   - fy4 * ((fx1_2 * fx2 + fx3_2 * fx1 + fx2_2 * fx3) - (fx1_2 * fx3 + fx2_2 * fx1 + fx3_2 * fx2));

                        //f2 = fx1_3 * ((fy2 * fx3 + fy3 * fx4 + fy4 * fx2) - (fy2 * fx4 + fy3 * fx2 + fy4 * fx3))
                        //   - fx2_3 * ((fy1 * fx3 + fy3 * fx4 + fy4 * fx1) - (fy1 * fx4 + fy3 * fx1 + fy4 * fx3))
                        //   + fx3_3 * ((fy1 * fx2 + fy2 * fx4 + fy4 * fx1) - (fy1 * fx4 + fy2 * fx1 + fy4 * fx2))
                        //   - fx4_3 * ((fy1 * fx2 + fy3 * fx1 + fy2 * fx3) - (fy1 * fx3 + fy2 * fx1 + fy3 * fx2));

                        //f3 = fx1_3 * ((fx2_2 * fy3 + fx3_2 * fy4 + fx4_2 * fy2) - (fx2_2 * fy4 + fx3_2 * fy2 + fx4_2 * fy3))
                        //   - fx2_3 * ((fx1_2 * fy3 + fx3_2 * fy4 + fx4_2 * fy1) - (fx1_2 * fy4 + fx3_2 * fy1 + fx4_2 * fy3))
                        //   + fx3_3 * ((fx1_2 * fy2 + fx2_2 * fy4 + fx4_2 * fy1) - (fx1_2 * fy4 + fx2_2 * fy1 + fx4_2 * fy2))
                        //   - fx4_3 * ((fx1_2 * fy2 + fx3_2 * fy1 + fx2_2 * fy3) - (fx1_2 * fy3 + fx2_2 * fy1 + fx3_2 * fy2));

                        f4 = fx1_3 * ((fx2_2 * fx3 * fy4 + fx3_2 * fx4 * fy2 + fx4_2 * fx2 * fy3) - (fx2_2 * fx4 * fy3 + fx3_2 * fx2 * fy4 + fx4_2 * fx3 * fy2))
                           - fx2_3 * ((fx1_2 * fx3 * fy4 + fx3_2 * fx4 * fy1 + fx4_2 * fx1 * fy3) - (fx1_2 * fx4 * fy3 + fx3_2 * fx1 * fy4 + fx4_2 * fx3 * fy1))
                           + fx3_3 * ((fx1_2 * fx2 * fy4 + fx2_2 * fx4 * fy1 + fx4_2 * fx1 * fy2) - (fx1_2 * fx4 * fy2 + fx2_2 * fx1 * fy4 + fx4_2 * fx2 * fy1))
                           - fx4_3 * ((fx1_2 * fx2 * fy3 + fx3_2 * fx1 * fy2 + fx2_2 * fx3 * fy1) - (fx1_2 * fx3 * fy2 + fx2_2 * fx1 * fy3 + fx3_2 * fx2 * fy1));

                        f0 = fx1_3 * ((fx2_2 * fx3       + fx3_2 * fx4       + fx4_2 * fx2      ) - (fx2_2 * fx4       + fx3_2 * fx2       + fx4_2 * fx3      ))
                           - fx2_3 * ((fx1_2 * fx3       + fx3_2 * fx4       + fx4_2 * fx1      ) - (fx1_2 * fx4       + fx3_2 * fx1       + fx4_2 * fx3      ))
                           + fx3_3 * ((fx1_2 * fx2       + fx2_2 * fx4       + fx4_2 * fx1      ) - (fx1_2 * fx4       + fx2_2 * fx1       + fx4_2 * fx2      ))
                           - fx4_3 * ((fx1_2 * fx2       + fx3_2 * fx1       + fx2_2 * fx3      ) - (fx1_2 * fx3       + fx2_2 * fx1       + fx3_2 * fx2      ));
                        #endregion

                        pfOCT[nLineOffset + nPoint] = (float)(f4 / f0);

                    }   // for (nPoint
                }   // for (nLine
            }   // for (nCalibrationLine
        }


        void applyDispersion(float[] pfOCT, float[] pfDispersionR, float[] pfDispersionI, ref float[] pfR, ref float[] pfI)
        {
            int nPoint, nLineLength = pfDispersionR.Length;
            float[] pfLine = new float[nLineLength];
            float[] pfLineR = new float[nLineLength];
            float[] pfLineI = new float[nLineLength];
            int nLine, nNumberLines = pfOCT.Length / nLineLength;
            for (nLine=0; nLine<nNumberLines; nLine++)
            {
                Buffer.BlockCopy(pfOCT, nLine * nLineLength * sizeof(float), pfLine, 0, nLineLength * sizeof(float));
                for (nPoint = 0; nPoint < nLineLength; nPoint++)
                {
                    pfLineR[nPoint] = pfLine[nPoint] * pfDispersionR[nPoint];
                    pfLineI[nPoint] = pfLine[nPoint] * pfDispersionI[nPoint];
                }   // for (nPoint
                Buffer.BlockCopy(pfLineR, 0, pfR, nLine * nLineLength * sizeof(float), nLineLength * sizeof(float));
                Buffer.BlockCopy(pfLineI, 0, pfI, nLine * nLineLength * sizeof(float), nLineLength * sizeof(float));
            }   // for (nLine
        }   // applyDispersion


        void getComplexDepthProfile(int nLineLength, float[] pfMask, ref float[] pfR, ref float[] pfI)
        {
            float[] pfLine = new float[nLineLength];
            int nLine, nPoint, nIndex, nNumberLines = pfR.Length / nLineLength;
            ComplexDouble[] pcdSpectrum = new ComplexDouble[nLineLength];
            ComplexDouble[] pcdDepthProfile = new ComplexDouble[nLineLength];

            for (nLine=0; nLine<nNumberLines; nLine++)
            {
                #region assemble complex line from pfR and pfI arrays
                for (nPoint=0; nPoint<nLineLength; nPoint++)
                {
                    nIndex = nLine * nLineLength + nPoint;
                    pcdSpectrum[nPoint].Real = pfMask[nPoint] * pfR[nIndex];
                    pcdSpectrum[nPoint].Imaginary = pfMask[nPoint] * pfI[nIndex];
                }   // for (nPoint
                #endregion

                #region complex fft
                pcdDepthProfile = NationalInstruments.Analysis.Dsp.Transforms.Fft(pcdSpectrum, false);
                #endregion

                #region assemble pfR and pfI lines
                for (nPoint = 0; nPoint < nLineLength; nPoint++)
                {
                    nIndex = nLine * nLineLength + nPoint;
                    pfR[nIndex] = (float) pcdDepthProfile[nPoint].Real;
                    pfI[nIndex] = (float) pcdDepthProfile[nPoint].Imaginary;
                }   // for (nPoint
                #endregion
            }   // for (nLine
        }


        void loadCalibration(string strFilename, int nNumberCalibrationLines, int nNumberOCTLinesPerCalibration, int nRawAlineLength, ref float[]pfK, ref int[] pnIndex)
        {
            try
            {
                FileStream fileStream = new FileStream(strFilename, FileMode.Open);
                BinaryReader binReader = new BinaryReader(fileStream);

                #region header

                int nTestNumberCalibrationLines = binReader.ReadInt32();
                int nTestNumberOCTLinesPerCalibration = binReader.ReadInt32();
                int nTestRawAlineLength = binReader.ReadInt32();

                #endregion header

                if ((nNumberCalibrationLines == nTestNumberCalibrationLines) && (nNumberOCTLinesPerCalibration == nTestNumberOCTLinesPerCalibration) && (nRawAlineLength == nTestRawAlineLength))
                {
                    int nSizeInBytes = (nNumberCalibrationLines * nRawAlineLength) * sizeof(float);
                    byte[] byteBuffer = new byte[nSizeInBytes];

                    #region pfK

                    byteBuffer = binReader.ReadBytes(nSizeInBytes);
                    Buffer.BlockCopy(byteBuffer, 0, pfK, 0, nSizeInBytes);

                    #endregion pfK

                    #region pnIndex

                    byteBuffer = binReader.ReadBytes(nSizeInBytes);
                    Buffer.BlockCopy(byteBuffer, 0, pnIndex, 0, nSizeInBytes);

                    #endregion pnIndex

                }
                fileStream.Close();
            }
            catch (Exception)
            {
            }
        }   // loadCalibration


        void saveCalibration(string strFilename, int nNumberCalibrationLines, int nNumberOCTLinesPerCalibration, int nRawAlineLength, float[] pfK, int[] pnIndex)
        {
            FileStream fileStream = new FileStream(strFilename, FileMode.Create);
            BinaryWriter binWriter = new BinaryWriter(fileStream);

            byte[] byteBuffer = new byte[pfK.Length * sizeof(float)];

            #region header

            binWriter.Write(nNumberCalibrationLines);
            binWriter.Write(nNumberOCTLinesPerCalibration);
            binWriter.Write(nRawAlineLength);

            #endregion header

            fileStream.Seek(1024, SeekOrigin.Begin);

            #region pfK

            Buffer.BlockCopy(pfK, 0, byteBuffer, 0, pfK.Length * sizeof(float));
            binWriter.Write(byteBuffer);

            #endregion pfK

            #region pnIndex

            Buffer.BlockCopy(pnIndex, 0, byteBuffer, 0, pnIndex.Length * sizeof(int));
            binWriter.Write(byteBuffer);

            #endregion pnIndex

            fileStream.Close();

        }   // loadCalibration


        void clearDispersion(ref float[] pfDispersionR, ref float[] pfDispersionI)
        {
            int nPoint, nLineLength = pfDispersionR.Length;
            for (nPoint = 0; nPoint < nLineLength; nPoint++)
            {
                pfDispersionR[nPoint] = 1.0f;
                pfDispersionI[nPoint] = 0.0f;
            }   // for (nPoint
        }   // clearDispersion


        void calculateDispersion(float[] pfData, float[] pfFFTMask, float[] pfMask, ref float[] pfDepthProfile, int nLeft, int nRight, ref float[] pfSpectrum, ref float[]pfPhase, ref float[] pfDispersionR, ref float[] pfDispersionI)
        {
            int nPoint, nLineLength = pfMask.Length;
            double[] pdLine = new double[nLineLength];
            ComplexDouble[] pcdFFT = new ComplexDouble[nLineLength];
            ComplexDouble[] pcdSpectrum = new ComplexDouble[nLineLength];
            float fTemp, fLeft, fRight, fSlope, fOffset;
            double[] pdFitLine = new double[nLineLength];

            #region multiply by FFT mask
            for (nPoint = 0; nPoint < nLineLength; nPoint++)
                pfData[nPoint] *= pfFFTMask[nPoint];
            #endregion multiply by FFT mask

            #region temp copy to spectrum plot
            Buffer.BlockCopy(pfData, 0, pfSpectrum, 0, nLineLength * sizeof(float));
            #endregion

            #region forward fft
            Array.Copy(pfData, 0, pdLine, 0, nLineLength);
            pcdFFT = NationalInstruments.Analysis.Dsp.Transforms.RealFft(pdLine);
            #endregion forward fft

            #region multiply mask and copy depth profile graph lines
            for (nPoint = 0; nPoint < nLineLength >> 1; nPoint++)
            {
                pfDepthProfile[0 * (nLineLength >> 1) + nPoint] = (float)(20.0 * Math.Log10(pcdFFT[nPoint].Magnitude));
                pcdFFT[nPoint].Real = pcdFFT[nPoint].Real * pfMask[nPoint];
                pcdFFT[nPoint].Imaginary = pcdFFT[nPoint].Imaginary * pfMask[nPoint];
                pfDepthProfile[1 * (nLineLength >> 1) + nPoint] = (float)(20.0 * Math.Log10(pcdFFT[nPoint].Magnitude));
            }   // for (nPoint
            for (nPoint = nLineLength >> 1; nPoint < nLineLength; nPoint++)
            {
                pcdFFT[nPoint].Real = pcdFFT[nPoint].Real * pfMask[nPoint];
                pcdFFT[nPoint].Imaginary = pcdFFT[nPoint].Imaginary * pfMask[nPoint];
            }   // for (nPoint
            #endregion multiply mask and copy depth profile graph lines

            #region inverse fft
            pcdSpectrum = NationalInstruments.Analysis.Dsp.Transforms.InverseFft(pcdFFT, false);
            #endregion inverse fft

            #region copy spectrum graph lines while calculating phase
            for (nPoint = 0; nPoint < nLeft; nPoint++)
            {
                pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                pfSpectrum[0 * nLineLength + nPoint] = fTemp;
                pfSpectrum[1 * nLineLength + nPoint] = Single.NaN;
            }
            for (nPoint = nLeft; nPoint < nRight; nPoint++)
            {
                pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                pfSpectrum[0 * nLineLength + nPoint] = fTemp;
                pfSpectrum[1 * nLineLength + nPoint] = fTemp;
            }
            for (nPoint = nRight; nPoint < nLineLength; nPoint++)
            {
                pdLine[nPoint] = pcdSpectrum[nPoint].Phase;
                fTemp = (float)(pcdSpectrum[nPoint].Magnitude);
                pfSpectrum[0 * nLineLength + nPoint] = fTemp;
                pfSpectrum[1 * nLineLength + nPoint] = Single.NaN;
            }
            #endregion copy spectrum graph lines while calculating phase
            #region unwrap phase
            NationalInstruments.Analysis.Dsp.SignalProcessing.UnwrapPhase(pdLine);
            #endregion unwrap phase

            #region calculate slope and offset from left and right edges
            fLeft = (float)pdLine[nLeft];
            fRight = (float)pdLine[nRight];
            fSlope = (fRight - fLeft) / (nRight - nLeft);
            fOffset = fLeft - fSlope * nLeft;
            #endregion calculate slope and offset from left and right edges

            #region calculate fit and difference curves, final dispersion arrays
            for (nPoint = 0; nPoint < nLineLength; nPoint++)
            {
                pfPhase[0 * nLineLength + nPoint] = (float)pdLine[nPoint];
                pfPhase[1 * nLineLength + nPoint] = fSlope * nPoint + fOffset;

                fTemp = pfPhase[1 * nLineLength + nPoint] - pfPhase[0 * nLineLength + nPoint];
                pfPhase[2 * nLineLength + nPoint] = fTemp;

                pfDispersionR[nPoint] = (float)Math.Cos(fTemp);
                pfDispersionI[nPoint] = (float)Math.Sin(fTemp);
            }   // for (nPoint
            #endregion calculate fit and difference curves, final dispersion arrays

        }   // calculateDispersion


        void loadDispersion(string strFilename, int nLineLength, ref float[] pfR, ref float[] pfI)
        {
            try
            {
                FileStream fileStream = new FileStream(strFilename, FileMode.Open);
                BinaryReader binReader = new BinaryReader(fileStream);

                #region header

                int nTestLineLength = binReader.ReadInt32();

                #endregion header

                if ((nLineLength == nTestLineLength))
                {
                    int nSizeInBytes = (nLineLength) * sizeof(float);
                    byte[] byteBuffer = new byte[nSizeInBytes];

                    #region R

                    byteBuffer = binReader.ReadBytes(nSizeInBytes);
                    Buffer.BlockCopy(byteBuffer, 0, pfR, 0, nSizeInBytes);

                    #endregion R

                    #region I

                    byteBuffer = binReader.ReadBytes(nSizeInBytes);
                    Buffer.BlockCopy(byteBuffer, 0, pfI, 0, nSizeInBytes);

                    #endregion I

                }
                fileStream.Close();
            }
            catch (Exception)
            {
            }
        }


        void saveDispersion(string strFilename, float[] pfR, float[] pfI)
        {
            int nLineLength = pfR.Length;

            FileStream fileStream = new FileStream(strFilename, FileMode.Create);
            BinaryWriter binWriter = new BinaryWriter(fileStream);

            byte[] byteBuffer = new byte[nLineLength * sizeof(float)];

            #region header

            binWriter.Write(nLineLength);

            #endregion header

            #region pfR

            Buffer.BlockCopy(pfR, 0, byteBuffer, 0, nLineLength * sizeof(float));
            binWriter.Write(byteBuffer);

            #endregion pfR

            #region pfI

            Buffer.BlockCopy(pfI, 0, byteBuffer, 0, nLineLength * sizeof(float));
            binWriter.Write(byteBuffer);

            #endregion pfI

            fileStream.Close();

        }


        void calculateMask(int nLeft, int nRight, int nRound, ref float[] pfMask)
        {
            int nPoint, nLineLength = pfMask.Length;

            #region error check mask parameters
            if (nRound < 1)
                nRound = 1;
            if (nRound > (nLineLength >> 2))
                nRound = nLineLength >> 2;

            if (nLeft < nRound)
                nLeft = nRound;
            if (nLeft > nLineLength - nRound - 1)
                nLeft = nLineLength - nRound - 1;

            if (nRight < nRound + 1)
                nRight = nRound + 1;
            if (nRight > nLineLength - nRound)
                nRight = nLineLength - nRound;

            if (nLeft >= nRight)
                nRight = nLeft + 1;

            #endregion get mask parameters (and error check)

            #region actual calculation

            Array.Clear(pfMask, 0, pfMask.Length);

            for (nPoint = 0; nPoint < nLeft - nRound; nPoint++)
                pfMask[nPoint] = 0.0f;

            for (nPoint = nLeft - nRound; nPoint < nLeft; nPoint++)
                pfMask[nPoint] = Convert.ToSingle(0.5 * (1.0 + Math.Cos(Math.PI * Convert.ToDouble(nLeft - nPoint) / Convert.ToDouble(nRound))));

            for (nPoint = nLeft; nPoint < nRight; nPoint++)
                pfMask[nPoint] = 1.0f;

            for (nPoint = nRight; nPoint < nRight + nRound; nPoint++)
                pfMask[nPoint] = Convert.ToSingle(0.5 * (1.0 + Math.Cos(Math.PI * Convert.ToDouble(nPoint - nRight) / Convert.ToDouble(nRound))));

            for (nPoint = nRight + nRound; nPoint < nLineLength; nPoint++)
                pfMask[nPoint] = 0.0f;

            #endregion actual calculation
        }   // calculateMask

        void calculateCalibration(float[] pfData, float[] pfMask, int nLineToShow, ref float[] pfDepthProfile)
        {
            #region initialization

            int nLineLength = pfMask.Length;
            int nNumberLines = pfData.Length / nLineLength;

            if (nLineToShow < 0) nLineToShow = 0;
            if (nLineToShow >= nNumberLines) nLineToShow = nNumberLines - 1;

            #endregion initialization

            #region allocations that should be pre-initialized

            double[] pdLine = new double[nLineLength];
            ComplexDouble[] pcdFFT = new ComplexDouble[nLineLength];

            #endregion allocations that should be pre-initialized

            for (int nLine = 0; nLine < nNumberLines; nLine++)
            {
                #region copy one float line to double array (prep for NI fft)
                Array.Copy(pfData, nLine * nLineLength, pdLine, 0, nLineLength);
                #endregion copy one float line to double array (prep for NI fft)

                #region forward fft
                pcdFFT = NationalInstruments.Analysis.Dsp.Transforms.RealFft(pdLine);
                #endregion forward fft

                #region copy non-masked depth profile of choice
                if (nLine == nLineToShow)
                    for (int nPoint = 0; nPoint < (nLineLength >> 1); nPoint++)
                        pfDepthProfile[0 * (nLineLength >> 1) + nPoint] = (float)(20.0 * Math.Log10(pcdFFT[nPoint].Magnitude));
                #endregion copy non-masked depth profile of choice

                #region multiply mask
                for (int nPoint =0; nPoint < (nLineLength >> 1); nPoint++)
                {
                    pcdFFT[nPoint].Real = pfMask[nPoint] * pcdFFT[nPoint].Real;
                    pcdFFT[nPoint].Imaginary = pfMask[nPoint] * pcdFFT[nPoint].Imaginary;
                }   // for (int nPoint
                for (int nPoint=(nLineLength >> 1); nPoint < nLineLength; nPoint++)
                {
                    pcdFFT[nPoint].Real = 0.0;
                    pcdFFT[nPoint].Imaginary = 0.0;
                }   // for (int nPoint
                #endregion multiply mask

                #region copy masked depth profile of choice
                if (nLine == nLineToShow)
                    for (int nPoint = 0; nPoint < (nLineLength >> 1); nPoint++)
                        pfDepthProfile[1 * (nLineLength >> 1) + nPoint] = (float)(20.0 * Math.Log10(pcdFFT[nPoint].Magnitude));
                #endregion copy masked depth profile of choice

            }   // for (int nLine

        }   // void calculateCalibration


        void Process1Thread()
        {

            #region initializing

            threadData.strProcess1ThreadStatus = "Initializing...";

            #region variables for thread operation

            bool bTroublemaker = false;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreProcess1Kill;
            pweStart[1] = threadData.mreProcess1Run;

            // wait handles for main loop
            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreProcess1Kill;
            pweLoop[1] = threadData.mreProcess1Action;

            #endregion variables for thread operation

            #region variables for main loop

            #region define general use variables

            int nPoint, n1LineLength = threadData.n1LineLength;
            int nSet, n1NumberCalibrationLines = threadData.n1NumberCalibrationLines;
            int n1NumberInterferenceLinesPerCalibration = threadData.n1NumberInterferenceLinesPerCalibration;

            int nLine, nLineCount, n1NumberLines = n1NumberCalibrationLines * n1NumberInterferenceLinesPerCalibration;
            int n1QuarterLines = n1NumberLines >> 2;
            float f1QuarterLines = (float)(n1QuarterLines);

            float[] pfLine = new float[n1LineLength];
            float[] pfSum = new float[n1LineLength];
            float[] pfTemp = new float[n1LineLength];

            #endregion define general use variables

            #region define reference variables

            float[] pfReference = new float[4 * n1LineLength];
            float[] pfReferenceRecorded = new float[4 * n1LineLength];

            Array.Clear(pfReference, 0, pfReference.Length);
            Array.Clear(pfReferenceRecorded, 0, pfReferenceRecorded.Length);

            #endregion define reference variables

            #region local calibration and interference arrays

            float[] pfCalibration = new float[n1NumberCalibrationLines * n1LineLength];
            float[] pfInterference = new float[n1NumberLines * n1LineLength];

            #endregion local calibration and interference arrays

            float[] pfCalibrationDepthProfile = new float[2 * (n1LineLength >> 1)];

            #region calibration data structures

            #region necessary for calculation

            //float[] pfCalibrationData = new float[nNumberSets * nLineLength];

            float[] pfCalibrationMask = new float[n1LineLength];

            //int nCalibrationPhaseLeft, nCalibrationPhaseRight;

            //float[] pfK = new float[nNumberSets * nLineLength];
            //int[] pnIndex = new int[nNumberSets * nLineLength];
            //clearCalibration(nNumberSets, ref pfK, ref pnIndex);
            //loadCalibration(UIData.strCalibrationFile, nNumberSets, nNumberLinesPerSet, nLineLength, ref pfK, ref pnIndex);

            #endregion necessary for calculation

            #region for display

            //float[] pfCalibrationSpectrum = new float[(2 * nNumberCalibrationDisplayLines) * nLineLength];
            //float[] pfCalibrationPhase = new float[(nNumberCalibrationDisplayLines + 1) * nLineLength];

            #endregion for display

            #endregion calibration data structures

            #region dispersion data structures

            #region necessary for calculation

            //float[] pfDispersionData = new float[nLineLength];

            //float[] pfDispersionMask = new float[nLineLength];

            //int nDispersionPhaseLeft, nDispersionPhaseRight;

            //float[] pfDispersionR = new float[nLineLength];
            //float[] pfDispersionI = new float[nLineLength];
            //clearDispersion(ref pfDispersionR, ref pfDispersionI);
            //loadDispersion(UIData.strDispersionFile, nLineLength, ref pfDispersionR, ref pfDispersionI);

            #endregion necessary for calculation

            #region for display

            //float[] pfDispersionDepthProfile = new float[2 * (nLineLength >> 1)];
            //float[] pfDispersionSpectrum = new float[2 * nLineLength];
            //float[] pfDispersionPhase = new float[3 * nLineLength];

            #endregion for display

            #endregion dispersion data structures

            #region OCT data structures

            #region necessary for calculation

            //float[] pfOCTData = new float[nNumberSets * nNumberLinesPerSet * nLineLength];

            //float[] pfR = new float[nNumberSets * nNumberLinesPerSet * nLineLength];
            //float[] pfI = new float[nNumberSets * nNumberLinesPerSet * nLineLength];

            //float[] pfFFTMask = new float[nLineLength];
            //calculateMask(UIData.nFFTMaskLeft, UIData.nFFTMaskRight, UIData.nFFTMaskRound, ref pfFFTMask);

            #endregion necessary for calculation

            #region for display

            //float[] pfIntensity = new float[nNumberLines * nLineLength];

            #endregion for display

            #endregion OCT data structures

            #endregion variables for main loop

            // initialization complete
            threadData.mreProcess1Ready.Set();
            threadData.strProcess1ThreadStatus = "Ready!";

            #endregion  // initializing

            #region main loop

            threadData.strProcess1ThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strProcess1ThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) != 0)
                {
                    threadData.mreProcess1Action.Reset();
                    threadData.strProcess1ThreadStatus = "try read lock!";
                    if (threadData.rwls1.TryEnterReadLock(threadData.nProcess1WriteTimeout) == true)
                    {
                        threadData.strProcess1ThreadStatus = "working...";
                        threadData.nProcess1Node = threadData.nProcessNode;

                        threadData.nProcess2Type = UIData.nURDisplayIndex;

                        #region read from process1 buffers, calculate and subtract reference, copy to appropriate local buffers

                        #region calculate reference spectra

                        switch (UIData.nLRReferenceMethod)
                        {
                            case 0:  // none
                                Array.Clear(pfReference, 0, pfReference.Length);
                                break;
                            case 1:  // use average
                                #region calculate averages of each quarter section

                                nLineCount = 0;
                                for (nSet = 0; nSet < 4; nSet++)
                                {
                                    Array.Clear(pfSum, 0, pfSum.Length);
                                    for (nLine = 0; nLine < f1QuarterLines; nLine++)
                                    {
                                        Buffer.BlockCopy(threadData.pf1Interference, nLineCount * n1LineLength * sizeof(float), pfLine, 0, pfLine.Length * sizeof(float));
                                        nLineCount++;
                                        pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();
                                    }   // for (nLine
                                    for (nPoint = 0; nPoint < n1LineLength; nPoint++)
                                        pfReference[nSet * n1LineLength + nPoint] = pfSum[nPoint] / f1QuarterLines;
                                }   // for (nSet

                                #endregion calculate average of each quarter section
                                break;
                            case 2:  // record
                                #region calculate averages of each quarter section

                                nLineCount = 0;
                                for (nSet = 0; nSet < 4; nSet++)
                                {
                                    Array.Clear(pfSum, 0, pfSum.Length);
                                    for (nLine = 0; nLine < f1QuarterLines; nLine++)
                                    {
                                        Buffer.BlockCopy(threadData.pf1Interference, nLineCount * n1LineLength * sizeof(float), pfLine, 0, pfLine.Length * sizeof(float));
                                        nLineCount++;
                                        pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();
                                    }   // for (nLine
                                    for (nPoint = 0; nPoint < n1LineLength; nPoint++)
                                        pfReference[nSet * n1LineLength + nPoint] = pfSum[nPoint] / f1QuarterLines;
                                }   // for (nSet

                                #endregion calculate average of each quarter section

                                Buffer.BlockCopy(pfReference, 0, pfReferenceRecorded, 0, pfReferenceRecorded.Length * sizeof(float));
                                break;
                            case 3:  // use recorded
                                Buffer.BlockCopy(pfReferenceRecorded, 0, pfReference, 0, pfReferenceRecorded.Length * sizeof(float));
                                break;
                        }

                        #endregion calculate reference spectra

                        #region transfer calibration and interference arrays from process1 buffers (with reference subtraction)

                        switch (threadData.nProcess1ProcessingType)
                        {
                            case 0:  // NI

                                nLineCount = 0;

                                for (nSet = 0; nSet < n1NumberCalibrationLines; nSet++)
                                {

                                    #region copy calibration line

                                    Buffer.BlockCopy(threadData.pf1Calibration, nSet * n1LineLength * sizeof(float), pfCalibration, nSet * n1LineLength * sizeof(float), n1LineLength * sizeof(float));

                                    #endregion copy calibration line

                                    #region copy interference section with reference subtracted

                                    #region copy appropriate reference line to pfTemp
                                    if ((nLineCount % n1QuarterLines) == 0)
                                        Buffer.BlockCopy(pfReference, (nLineCount / n1QuarterLines) * n1LineLength * sizeof(float), pfTemp, 0, pfTemp.Length * sizeof(float));
                                    #endregion copy appropriate reference line to pfTemp

                                    for (nLine = 0; nLine < n1NumberInterferenceLinesPerCalibration; nLine++)
                                    {
                                        #region copy line from pf1Interference to pfLine
                                        Buffer.BlockCopy(threadData.pf1Interference, nLineCount * n1LineLength * sizeof(float), pfLine, 0, pfLine.Length * sizeof(float));
                                        #endregion copy line from pf1Interference to pfLine

                                        #region subtract reference array from pfLine
                                        pfLine = (pfLine.Zip(pfTemp, (x, y) => x - y)).ToArray();
                                        #endregion subtract reference array from pfLine

                                        #region copy subtracted line to local interference array
                                        Buffer.BlockCopy(pfLine, 0, pfInterference, nLineCount * n1LineLength * sizeof(float), pfLine.Length * sizeof(float));
                                        #endregion copy subtracted line to local interference array

                                        nLineCount++;
                                    }   // for (nLine

                                    #endregion copy interference section with reference subtracted

                                }   // for (nSet

                                break;
                            case 1:  // IPP

                                // IPP dll call to copy data to IPP memory, pass back reference and reference recorded arrays

                                break;
                            case 2:  // CUDA

                                // CUDA dll call to copy data to CUDA host memory, pass back reference and reference recorded arrays

                                break;
                        }

                        #endregion transfer calibration and interference arrays from process1 buffers (with reference subtraction)

                        #endregion  read from process1 buffers, calculate and subtract reference, copy to appropriate local buffers

                        #region copy raw data buffers to process2 for spectral binning

                        if ((threadData.nProcess2Type == 8) && (UIData.nLLSystemType == 1))
                        {
                            Buffer.BlockCopy(threadData.pf1Calibration, 0, threadData.pf2Calibration, 0, threadData.pf1Calibration.Length * sizeof(float));
                            Buffer.BlockCopy(threadData.pf1Interference, 0, threadData.pf2Interference, 0, threadData.pf1Interference.Length * sizeof(float));
                        }   // if ((threadData.nProcess2Type

                        #endregion copy raw data buffers to process2 for spectral binning

                        threadData.rwls1.ExitReadLock();

                        #region copy reference arrays to graph (if requested)

                        if (UIData.bLRReferenceActive)
                            switch (UIData.nLRReferenceDisplay)
                            {
                                case 0:  // all
                                    Buffer.BlockCopy(pfReference, 0 * n1LineLength * sizeof(float), UIData.pfReference, 0 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Buffer.BlockCopy(pfReference, 1 * n1LineLength * sizeof(float), UIData.pfReference, 1 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Buffer.BlockCopy(pfReference, 2 * n1LineLength * sizeof(float), UIData.pfReference, 2 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Buffer.BlockCopy(pfReference, 3 * n1LineLength * sizeof(float), UIData.pfReference, 3 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    break;
                                case 1:  // parallel even
                                    Buffer.BlockCopy(pfReference, 0 * n1LineLength * sizeof(float), UIData.pfReference, 0 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Array.Clear(UIData.pfReference, 1 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 2 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 3 * n1LineLength, n1LineLength);
                                    break;
                                case 2:  // parallel odd
                                    Array.Clear(UIData.pfReference, 0 * n1LineLength, n1LineLength);
                                    Buffer.BlockCopy(pfReference, 1 * n1LineLength * sizeof(float), UIData.pfReference, 1 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Array.Clear(UIData.pfReference, 2 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 3 * n1LineLength, n1LineLength);
                                    break;
                                case 3:  // perpendicular even
                                    Array.Clear(UIData.pfReference, 0 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 1 * n1LineLength, n1LineLength);
                                    Buffer.BlockCopy(pfReference, 2 * n1LineLength * sizeof(float), UIData.pfReference, 2 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    Array.Clear(UIData.pfReference, 3 * n1LineLength, n1LineLength);
                                    break;
                                case 4:  // perpendicular odd
                                    Array.Clear(UIData.pfReference, 0 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 1 * n1LineLength, n1LineLength);
                                    Array.Clear(UIData.pfReference, 2 * n1LineLength, n1LineLength);
                                    Buffer.BlockCopy(pfReference, 3 * n1LineLength * sizeof(float), UIData.pfReference, 3 * n1LineLength * sizeof(float), n1LineLength * sizeof(float));
                                    break;
                            }   // switch (UIData.nLRCalibrationReferenceDisplay

                        #endregion copy reference arrays to graph (if requested)

                        #region calibration (if selected)

                        if (UIData.bCalibrationActive)
                        {

                            #region calculate mask
                            calculateMask(UIData.nCalibrationDepthLeft, UIData.nCalibrationDepthRight, UIData.nCalibrationDepthRound, ref pfCalibrationMask);
                            #endregion calculate mask

                            switch (threadData.nProcess1ProcessingType)
                            {
                                case 0:  // NI
                                    break;
                                case 1:  // IPP
                                    break;
                                case 2:  // CUDA
                                    break;
                            }   // switch (threadData.nProcess1ProcessingType

                        }   // if (UIData.bCalibrationActive


                        #endregion calibration (if selected)






                        //#region actual processing

                        ////#region calculate calibration if selected
                        ////if (UIData.bCalibrationActive)
                        ////{
                        ////    #region get left / right phase parameters (and error check)
                        ////    nCalibrationPhaseLeft = UIData.nCalibrationPhaseLeft;
                        ////    if (nCalibrationPhaseLeft < 0)
                        ////        nCalibrationPhaseLeft = 0;
                        ////    if (nCalibrationPhaseLeft > (pfCalibrationMask.Length - 2))
                        ////        nCalibrationPhaseLeft = (pfCalibrationMask.Length - 2);

                        ////    nCalibrationPhaseRight = UIData.nCalibrationPhaseRight;
                        ////    if (nCalibrationPhaseRight < 1)
                        ////        nCalibrationPhaseRight = 1;
                        ////    if (nCalibrationPhaseRight > (pfCalibrationMask.Length - 1))
                        ////        nCalibrationPhaseRight = (pfCalibrationMask.Length - 1);

                        ////    if (nCalibrationPhaseLeft >= nCalibrationPhaseRight)
                        ////        nCalibrationPhaseRight = nCalibrationPhaseLeft + 1;
                        ////    #endregion get left / right phase parameters (and error check)
                        ////    #region calculate calibration
                        ////    switch (threadData.nProcess1ProcessingType)
                        ////    {
                        ////        case 0:  // NI
                        ////            calculateCalibration(nNumberSets, pfCalibrationData, pfCalibrationMask, ref pfCalibrationDepthProfile, nCalibrationPhaseLeft, nCalibrationPhaseRight, ref pfCalibrationSpectrum, ref pfCalibrationPhase, ref pfK, ref pnIndex);
                        ////            break;
                        ////        case 1:  // IPP
                        ////            #if TRUEIPP
                        ////            // call ipp function
                        ////            Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////            #endif  // TRUEIPP
                        ////            break;
                        ////        case 2:  // CUDA
                        ////            #if TRUECUDA
                        ////            Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////            #endif  // TRUECUDA
                        ////            break;
                        ////    }  // switch (threadData.nProcess1ProcessingType
                        ////    #endregion  // calculate calibration
                        ////    #region send results to graph
                        ////    Buffer.BlockCopy(pfCalibrationDepthProfile, 0, UIData.pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length * sizeof(float));
                        ////    Buffer.BlockCopy(pfCalibrationSpectrum, 0, UIData.pfCalibrationSpectrum, 0, pfCalibrationSpectrum.Length * sizeof(float));
                        ////    Buffer.BlockCopy(pfCalibrationPhase, 0, UIData.pfCalibrationPhase, 0, pfCalibrationPhase.Length * sizeof(float));
                        ////    #endregion  // send results to graph
                        ////}
                        ////#endregion calculate calibration if selected

                        ////#region save, load, or clear calibration files if requested

                        ////#region if load
                        ////if (UIData.bCalibrationLoad)
                        ////{
                        ////    loadCalibration(UIData.strCalibrationFile, nNumberSets, nNumberLinesPerSet, nLineLength, ref pfK, ref pnIndex);

                        ////    UIData.bCalibrationLoad = false;
                        ////    UIData.bCalibrationSave = false;
                        ////    UIData.bCalibrationClear = false;
                        ////}   // if (UIData.bCalibrationLoad
                        ////#endregion if load

                        ////#region if save
                        ////if (UIData.bCalibrationSave)
                        ////{
                        ////    saveCalibration(UIData.strCalibrationFile, nNumberSets, nNumberLinesPerSet, nLineLength, pfK, pnIndex);

                        ////    UIData.bCalibrationLoad = false;
                        ////    UIData.bCalibrationSave = false;
                        ////    UIData.bCalibrationClear = false;
                        ////}   // if (UIData.bCalibrationLoad
                        ////#endregion if save

                        ////#region if clear
                        ////if (UIData.bCalibrationClear)
                        ////{
                        ////    clearCalibration(nNumberSets, ref pfK, ref pnIndex);

                        ////    UIData.bCalibrationLoad = false;
                        ////    UIData.bCalibrationSave = false;
                        ////    UIData.bCalibrationClear = false;
                        ////}   // if (UIData.bCalibrationLoad
                        ////#endregion if clear

                        ////#endregion save, load, or clear calibration files if requested

                        ////#region apply calibration
                        ////switch (threadData.nProcess1ProcessingType)
                        ////{
                        ////    case 0:  // NI
                        ////        applyCalibration(nNumberSets, nNumberLinesPerSet, ref pfOCTData, pfK, pnIndex);
                        ////        // in other cases, the calibrated data stays in the dll, so just 'pfOCTData', not 'ref pfOCTData'
                        ////        break;
                        ////    case 1:  // IPP
                        ////        #if TRUEIPP
                        ////        // call ipp function
                        ////        Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////        #endif  // TRUEIPP
                        ////        break;
                        ////    case 2:  // CUDA
                        ////        #if TRUECUDA
                        ////        Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////        #endif  // TRUECUDA
                        ////        break;
                        ////}  // switch (threadData.nProcess1ProcessingType
                        ////#endregion  // apply calibration

                        ////#region calculate dispersion if selected
                        ////if (UIData.bDispersionActive)
                        ////{
                        ////    #region get data for dispersion compensation calculation
                        ////    if (UIData.nDispersionLine == -1)
                        ////    {
                        ////        #region calculate average for OCT data
                        ////        int nSet = 0;
                        ////        Array.Clear(pfSum, 0, pfSum.Length);
                        ////        for (nAline = 0; nAline < nNumberLinesPerSet; nAline++)
                        ////        {
                        ////            Buffer.BlockCopy(pfOCTData, (nSet * nNumberLinesPerSet + nAline) * nLineLength * sizeof(float), pfLine, 0, nLineLength * sizeof(float));
                        ////            pfSum = (pfSum.Zip(pfLine, (x, y) => x + y)).ToArray();
                        ////        }   // for (nAline
                        ////        for (nPoint = 0; nPoint < nLineLength; nPoint++)
                        ////            pfDispersionData[nPoint] = pfSum[nPoint] / ((float)nNumberLinesPerSet);
                        ////        #endregion calculate average for OCT data in each set
                        ////    }
                        ////    else
                        ////    {
                        ////        #region error checking on line number
                        ////        nAline = UIData.nDispersionLine;
                        ////        if (nAline < 0)
                        ////            nAline = 0;
                        ////        if (nAline > nNumberLines - 1)
                        ////            nAline = nNumberLines - 1;
                        ////        #endregion error checking on line number
                        ////        #region get line
                        ////        switch (UIData.nLLSystemType)
                        ////        {
                        ////            case 0: // SD-OCT
                        ////                Buffer.BlockCopy(pfOCTData, nAline * nLineLength * sizeof(float), pfDispersionData, 0, nLineLength * sizeof(float));
                        ////                break;
                        ////            case 1: // PS SD-OCT
                        ////                int nSet = nAline % 2;
                        ////                Buffer.BlockCopy(pfOCTData, (nSet * nNumberLinesPerSet + (nAline >> 1)) * nLineLength * sizeof(float), pfDispersionData, 0, nLineLength * sizeof(float));
                        ////                break;
                        ////            case 2: // line field
                        ////                break;
                        ////            case 3: // OFDI
                        ////                break;
                        ////            case 4: // PS OFDI
                        ////                break;
                        ////        }   // switch (UIData.nLLSystemType
                        ////        #endregion get line
                        ////    }
                        ////    #endregion get data for dispersion compensation calculation

                        ////    #region calculate mask
                        ////    calculateMask(UIData.nDispersionDepthLeft, UIData.nDispersionDepthRight, UIData.nDispersionDepthRound, ref pfDispersionMask);
                        ////    #endregion  // calculate mask
                        ////    #region get left / right phase parameters (and error check)
                        ////    nDispersionPhaseLeft = UIData.nDispersionPhaseLeft;
                        ////    if (nDispersionPhaseLeft < 0)
                        ////        nDispersionPhaseLeft = 0;
                        ////    if (nDispersionPhaseLeft > (pfDispersionMask.Length - 2))
                        ////        nDispersionPhaseLeft = (pfDispersionMask.Length - 2);

                        ////    nDispersionPhaseRight = UIData.nDispersionPhaseRight;
                        ////    if (nDispersionPhaseRight < 1)
                        ////        nDispersionPhaseRight = 1;
                        ////    if (nDispersionPhaseRight > (pfDispersionMask.Length - 1))
                        ////        nDispersionPhaseRight = (pfDispersionMask.Length - 1);

                        ////    if (nDispersionPhaseLeft >= nDispersionPhaseRight)
                        ////        nDispersionPhaseRight = nDispersionPhaseLeft + 1;
                        ////    #endregion get left / right phase parameters (and error check)
                        ////    #region calculate Dispersion
                        ////    switch (threadData.nProcess1ProcessingType)
                        ////    {
                        ////        case 0:  // NI
                        ////            calculateDispersion(pfDispersionData, pfFFTMask, pfDispersionMask, ref pfDispersionDepthProfile, nDispersionPhaseLeft, nDispersionPhaseRight, ref pfDispersionSpectrum, ref pfDispersionPhase, ref pfDispersionR, ref pfDispersionI);
                        ////            break;
                        ////        case 1:  // IPP
                        ////            #if TRUEIPP
                        ////            // call ipp function
                        ////            Array.Clear(pfDispersionDepthProfile, 0, pfDispersionDepthProfile.Length);
                        ////            #endif  // TRUEIPP
                        ////            break;
                        ////        case 2:  // CUDA
                        ////            #if TRUECUDA
                        ////            Array.Clear(pfDispersionDepthProfile, 0, pfDispersionDepthProfile.Length);
                        ////            #endif  // TRUECUDA
                        ////            break;
                        ////    }  // switch (threadData.nProcess1ProcessingType
                        ////    #endregion  // calculate Dispersion
                        ////    #region send results to graph
                        ////    Buffer.BlockCopy(pfDispersionDepthProfile, 0, UIData.pfDispersionDepthProfile, 0, pfDispersionDepthProfile.Length * sizeof(float));
                        ////    Buffer.BlockCopy(pfDispersionSpectrum, 0, UIData.pfDispersionSpectrum, 0, pfDispersionSpectrum.Length * sizeof(float));
                        ////    Buffer.BlockCopy(pfDispersionPhase, 0, UIData.pfDispersionPhase, 0, pfDispersionPhase.Length * sizeof(float));
                        ////    #endregion  // send results to graph
                        ////}
                        ////#endregion calculate Dispersion if selected

                        ////#region save, load, or clear Dispersion files if requested

                        ////#region if load
                        ////if (UIData.bDispersionLoad)
                        ////{
                        ////    loadDispersion(UIData.strDispersionFile, nLineLength, ref pfDispersionR, ref pfDispersionI);

                        ////    UIData.bDispersionLoad = false;
                        ////    UIData.bDispersionSave = false;
                        ////    UIData.bDispersionClear = false;
                        ////}   // if (UIData.bDispersionLoad
                        ////#endregion if load

                        ////#region if save
                        ////if (UIData.bDispersionSave)
                        ////{
                        ////    saveDispersion(UIData.strDispersionFile, pfDispersionR, pfDispersionI);

                        ////    UIData.bDispersionLoad = false;
                        ////    UIData.bDispersionSave = false;
                        ////    UIData.bDispersionClear = false;
                        ////}   // if (UIData.bDispersionLoad
                        ////#endregion if save

                        ////#region if clear
                        ////if (UIData.bDispersionClear)
                        ////{
                        ////    clearDispersion(ref pfDispersionR, ref pfDispersionI);

                        ////    UIData.bDispersionLoad = false;
                        ////    UIData.bDispersionSave = false;
                        ////    UIData.bDispersionClear = false;
                        ////}   // if (UIData.bDispersionLoad
                        ////#endregion if clear

                        ////#endregion save, load, or clear Dispersion files if requested

                        ////#region apply Dispersion
                        ////switch (threadData.nProcess1ProcessingType)
                        ////{
                        ////    case 0:  // NI
                        ////        applyDispersion(pfOCTData, pfDispersionR, pfDispersionI, ref pfR, ref pfI);
                        ////        // in other cases, the calibrated data stays in the dll, so just 'pfOCTData', not 'ref pfOCTData'
                        ////        break;
                        ////    case 1:  // IPP
                        ////        #if TRUEIPP
                        ////        // call ipp function
                        ////        Array.Clear(pfDispersionDepthProfile, 0, pfDispersionDepthProfile.Length);
                        ////        #endif  // TRUEIPP
                        ////        break;
                        ////    case 2:  // CUDA
                        ////        #if TRUECUDA
                        ////        Array.Clear(pfDispersionDepthProfile, 0, pfDispersionDepthProfile.Length);
                        ////        #endif  // TRUECUDA
                        ////        break;
                        ////}  // switch (threadData.nProcess1ProcessingType
                        ////#endregion  // apply Dispersion

                        ////#region get final results
                        ////if (UIData.bFFTMaskUpdated)
                        ////{
                        ////    #region calculate mask
                        ////    calculateMask(UIData.nFFTMaskLeft, UIData.nFFTMaskRight, UIData.nFFTMaskRound, ref pfFFTMask);
                        ////    #endregion  // calculate mask
                        ////}   // if (UIData.bFFTMaskUpdated

                        ////switch (threadData.nProcess1ProcessingType)
                        ////{
                        ////    case 0:  // NI
                        ////        getComplexDepthProfile(threadData.nRawAlineLength, pfFFTMask, ref pfR, ref pfI);
                        ////        break;
                        ////    case 1:  // IPP
                        ////        #if TRUEIPP
                        ////        // call ipp function
                        ////        Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////        #endif  // TRUEIPP
                        ////        break;
                        ////    case 2:  // CUDA
                        ////        #if TRUECUDA
                        ////        Array.Clear(pfCalibrationDepthProfile, 0, pfCalibrationDepthProfile.Length);
                        ////        #endif  // TRUECUDA
                        ////        break;
                        ////}  // switch (threadData.nProcess1ProcessingType
                        ////#endregion  // final results

                        //#endregion  // actual processing

                        //#region launch process2

                        //if (threadData.rwlsProcess1To2.TryEnterWriteLock(1000))
                        //{

                        //    #region prepare secondary processing


                        //    switch (threadData.nProcess2Type)
                        //    {
                        //        case 0:  // none
                        //            break;
                        //        case 1:  // intensity
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 2:  // attenuation
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 3:  // phase
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 4:  // polarization
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 5:  // angiography
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 6:  // elastography
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 7:  // spectroscopy
                        //            // copy results to pnProcess2 data structures
                        //            break;
                        //        case 8:  // spectral binning
                        //            // already taken care of previously
                        //            break;
                        //    }

                        //    #endregion  // prepare secondary processing

                        //    threadData.rwlsProcess1To2.ExitWriteLock();
                        //    threadData.mreProcess2Action.Set();
                        //}
                        //else
                        //{
                        //    bTroublemaker = true;
                        //    threadData.strProcess1ThreadStatus = "1 to 2 timeout!";
                        //    threadData.mreProcess1Kill.Set();
                        //    threadData.mreMainKill.Set();
                        //}

                        //#endregion launch process2

                        //#region send out intensity updates

                        ////#region check for UL intensity

                        ////if (UIData.nULDisplayIndex == 3)
                        ////{
                        ////    #region main

                        ////    #region get preferences from UI

                        ////    bool bEven = false, bOdd = false;
                        ////    bool bParallel = false, bPerpendicular = false;
                        ////    bool[] pbCalibration = new bool[4];

                        ////    int nCalibrationLine, nLine;
                        ////    int nLineOffset;
                        ////    int nDoubler, nDoubleLines = 1;

                        ////    #region even/odd line selection
                        ////    switch (UIData.nULIntensityLines)
                        ////    {
                        ////        case 0:  // all lines
                        ////            bEven = true;
                        ////            bOdd = true;
                        ////            break;
                        ////        case 1:  // even lines
                        ////            bEven = true;
                        ////            bOdd = false;
                        ////            break;
                        ////        case 2:  // odd lines
                        ////            bEven = false;
                        ////            bOdd = true;
                        ////            break;
                        ////    }   // switch (UIData.nULIntensityLines
                        ////    #endregion even/odd line selection

                        ////    #region camera selection
                        ////    switch (UIData.nULIntensityCamera)
                        ////    {
                        ////        case 0:  // both cameras
                        ////            bParallel = true;
                        ////            bPerpendicular = true;
                        ////            break;
                        ////        case 1:  // parallel
                        ////            bParallel = true;
                        ////            bPerpendicular = false;
                        ////            break;
                        ////        case 2:  // perpendicular
                        ////            bParallel = false;
                        ////            bPerpendicular = true;
                        ////            break;
                        ////    }   // switch (UIData.nULIntensityCamera
                        ////    #endregion camera selection

                        ////    #region combine selections

                        ////    if (UIData.nLLSystemType == 1)  // if PS-OCT
                        ////    {
                        ////        pbCalibration[0] = bEven && bParallel;
                        ////        pbCalibration[1] = bOdd && bParallel;
                        ////        pbCalibration[2] = bEven && bPerpendicular;
                        ////        pbCalibration[3] = bOdd && bPerpendicular;
                        ////        nDoubleLines = 2;
                        ////    }
                        ////    else  // if (UIData.nLLSystemType
                        ////    {
                        ////        pbCalibration[0] = true;
                        ////        pbCalibration[1] = false;
                        ////        pbCalibration[2] = false;
                        ////        pbCalibration[3] = false;
                        ////        nDoubleLines = 1;
                        ////    }  // if (UIData.nLLSystemType

                        ////    #endregion combine selections

                        ////    #endregion get preferences from UI

                        ////    Array.Clear(pfIntensity, 0, pfIntensity.Length);

                        ////    for (nLine=0; nLine<nNumberLinesPerSet; nLine++)
                        ////    {
                        ////        Array.Clear(pfLine, 0, pfLine.Length);
                        ////        for (nCalibrationLine=0; nCalibrationLine < nNumberSets; nCalibrationLine++)
                        ////        {
                        ////            if (pbCalibration[nCalibrationLine])
                        ////            {
                        ////                nLineOffset = (nCalibrationLine * nNumberLinesPerSet + nLine) * nLineLength;
                        ////                for (nPoint = 0; nPoint < nLineLength; nPoint++)
                        ////                    pfLine[nPoint] += pfR[nLineOffset + nPoint] * pfR[nLineOffset + nPoint] + pfI[nLineOffset + nPoint] * pfI[nLineOffset + nPoint];
                        ////            }   // if (pbCalibrationLine
                        ////        }   // for (nCalibrationLine
                        ////        for (nPoint = 0; nPoint < nLineLength >> 1; nPoint++)
                        ////        {
                        ////            for (nDoubler = 0; nDoubler < nDoubleLines; nDoubler++)
                        ////            {
                        ////                UIData.pfULImage[nDoubleLines * nLine + nDoubler, 2 * nPoint + 0] = (float)(10.0 * Math.Log10(pfLine[nPoint]));
                        ////                UIData.pfULImage[nDoubleLines * nLine + nDoubler, 2 * nPoint + 1] = UIData.pfULImage[nLine, 2 * nPoint + 0];
                        ////            }   // for (nDoubler
                        ////        }
                        ////    }   // for (nLine

                        ////    #endregion main

                        ////    #region left
                        ////    nAline = UIData.nULLeft;
                        ////    if (nAline < 0) nAline = 0;
                        ////    if (nAline >= threadData.nRawNumberAlines) nAline = threadData.nRawNumberAlines - 1;
                        ////    for (nPoint = 0; nPoint < threadData.nRawAlineLength; nPoint++)
                        ////        UIData.pfULLeft[0, nPoint] = UIData.pfULImage[nAline, nPoint];
                        ////    #endregion

                        ////    #region top
                        ////    nPoint = UIData.nULTop;
                        ////    if (nPoint < 0) nPoint = 0;
                        ////    if (nPoint >= threadData.nRawAlineLength) nPoint = threadData.nRawAlineLength - 1;
                        ////    for (nAline = 0; nAline < threadData.nRawNumberAlines; nAline++)
                        ////        UIData.pfULTop[0, nAline] = UIData.pfULImage[nAline, nPoint];
                        ////    #endregion
                        ////}
                        ////#endregion check for UL intensity

                        ////#region check for UR intensity

                        ////if (UIData.nURDisplayIndex == 1)
                        ////{
                        ////    #region main

                        ////    #region get preferences from UI

                        ////    bool bEven = false, bOdd = false;
                        ////    bool bParallel = false, bPerpendicURar = false;
                        ////    bool[] pbCalibration = new bool[4];

                        ////    int nCalibrationLine, nLine;
                        ////    int nLineOffset;
                        ////    int nDoubler, nDoubleLines = 1;

                        ////    #region even/odd line selection
                        ////    switch (UIData.nURIntensityLines)
                        ////    {
                        ////        case 0:  // all lines
                        ////            bEven = true;
                        ////            bOdd = true;
                        ////            break;
                        ////        case 1:  // even lines
                        ////            bEven = true;
                        ////            bOdd = false;
                        ////            break;
                        ////        case 2:  // odd lines
                        ////            bEven = false;
                        ////            bOdd = true;
                        ////            break;
                        ////    }   // switch (UIData.nURIntensityLines
                        ////    #endregion even/odd line selection

                        ////    #region camera selection
                        ////    switch (UIData.nURIntensityCamera)
                        ////    {
                        ////        case 0:  // both cameras
                        ////            bParallel = true;
                        ////            bPerpendicURar = true;
                        ////            break;
                        ////        case 1:  // parallel
                        ////            bParallel = true;
                        ////            bPerpendicURar = false;
                        ////            break;
                        ////        case 2:  // perpendicURar
                        ////            bParallel = false;
                        ////            bPerpendicURar = true;
                        ////            break;
                        ////    }   // switch (UIData.nURIntensityCamera
                        ////    #endregion camera selection

                        ////    #region combine selections

                        ////    if (UIData.nLLSystemType == 1)  // if PS-OCT
                        ////    {
                        ////        pbCalibration[0] = bEven && bParallel;
                        ////        pbCalibration[1] = bOdd && bParallel;
                        ////        pbCalibration[2] = bEven && bPerpendicURar;
                        ////        pbCalibration[3] = bOdd && bPerpendicURar;
                        ////        nDoubleLines = 2;
                        ////    }
                        ////    else  // if (UIData.nLLSystemType
                        ////    {
                        ////        pbCalibration[0] = true;
                        ////        pbCalibration[1] = false;
                        ////        pbCalibration[2] = false;
                        ////        pbCalibration[3] = false;
                        ////        nDoubleLines = 1;
                        ////    }  // if (UIData.nLLSystemType

                        ////    #endregion combine selections

                        ////    #endregion get preferences from UI

                        ////    Array.Clear(pfIntensity, 0, pfIntensity.Length);

                        ////    for (nLine = 0; nLine < nNumberLinesPerSet; nLine++)
                        ////    {
                        ////        Array.Clear(pfLine, 0, pfLine.Length);
                        ////        for (nCalibrationLine = 0; nCalibrationLine < nNumberSets; nCalibrationLine++)
                        ////        {
                        ////            if (pbCalibration[nCalibrationLine])
                        ////            {
                        ////                nLineOffset = (nCalibrationLine * nNumberLinesPerSet + nLine) * nLineLength;
                        ////                for (nPoint = 0; nPoint < nLineLength >> 1; nPoint++)
                        ////                    pfLine[nPoint] += pfR[nLineOffset + nPoint] * pfR[nLineOffset + nPoint] + pfI[nLineOffset + nPoint] * pfI[nLineOffset + nPoint];
                        ////            }   // if (pbCalibrationLine
                        ////        }   // for (nCalibrationLine
                        ////        for (nPoint = 0; nPoint < nLineLength >> 1; nPoint++)
                        ////        {
                        ////            for (nDoubler = 0; nDoubler < nDoubleLines; nDoubler++)
                        ////                UIData.pfURImage[nDoubleLines * nLine + nDoubler, nPoint] = (float)(10.0 * Math.Log10(pfLine[nPoint]));
                        ////        }
                        ////    }   // for (nLine

                        ////    #endregion main

                        ////    #region left
                        ////    nAline = UIData.nURLeft;
                        ////    if (nAline < 0) nAline = 0;
                        ////    if (nAline >= threadData.nRawNumberAlines) nAline = threadData.nRawNumberAlines - 1;
                        ////    for (nPoint = 0; nPoint < threadData.nRawAlineLength >> 1; nPoint++)
                        ////        UIData.pfURLeft[0, nPoint] = UIData.pfURImage[nAline, nPoint];
                        ////    #endregion

                        ////    #region top
                        ////    nPoint = UIData.nURTop;
                        ////    if (nPoint < 0) nPoint = 0;
                        ////    if (nPoint >= threadData.nRawAlineLength >> 1) nPoint = (threadData.nRawAlineLength >> 1) - 1;
                        ////    for (nAline = 0; nAline < threadData.nRawNumberAlines; nAline++)
                        ////        UIData.pfURTop[0, nAline] = UIData.pfURImage[nAline, nPoint];
                        ////    #endregion
                        ////}
                        ////#endregion check for UR intensity

                        //#endregion  // send out intensity updates

                        threadData.strProcess1ThreadStatus = "done!";
                    }
                    else
                    {
                        bTroublemaker = true;
                        threadData.strProcess1ThreadStatus = "problem!";
                        threadData.mreProcess1Kill.Set();
                        threadData.mreMainKill.Set();
                    }

                }

            }  // if (WaitHandle.WaitAny
            #endregion

            #region cleanup

            if (bTroublemaker)
            {
                threadData.mreProcess1Dead.Set();
            }
            else  // if (bTroublemaker
            {
                threadData.strProcess1ThreadStatus = "Cleaning up...";
                // clean up code
                threadData.nProcess1Node = -1;
                // signal other threads
                threadData.mreProcess1Dead.Set();
                threadData.strProcess1ThreadStatus = "Done.";
            }  // if (bTroublemaker

            #endregion
        }


        void Process2Thread()
        {
            #region initializing
            threadData.strProcess2ThreadStatus = "Initializing...";

            // initialization
            bool bTroublemaker = false;

            int nProcess2Type;

            int nAline, nPoint;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreProcess2Kill;
            pweStart[1] = threadData.mreProcess2Run;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreProcess2Kill;
            pweLoop[1] = threadData.mreProcess2Action;

            // initialization complete
            threadData.mreProcess2Ready.Set();
            threadData.strProcess2ThreadStatus = "Ready!";
            #endregion

            #region main loop
            threadData.strProcess2ThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strProcess2ThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) != 0)
                {
                    threadData.mreProcess2Action.Reset();
                    threadData.strProcess2ThreadStatus = "try read lock!";
                    if (threadData.rwlsProcess1To2.TryEnterReadLock(1000))
                    {
                        threadData.strProcess2ThreadStatus = "working...";
                        threadData.nProcess2Node = threadData.nProcess1Node;
                        nProcess2Type = threadData.nProcess2Type;

                        switch (nProcess2Type)
                        {
                            case 0:
                                threadData.strProcess2ThreadStatus = "...none...";
                                break;
                            case 1:
                                threadData.strProcess2ThreadStatus = "...intensity...";
                                break;
                            case 2:
                                threadData.strProcess2ThreadStatus = "...attenuation...";
                                break;
                            case 3:
                                threadData.strProcess2ThreadStatus = "...phase...";
                                break;
                            case 4:
                                threadData.strProcess2ThreadStatus = "...polarization...";
                                break;
                            case 5:
                                threadData.strProcess2ThreadStatus = "...angiography...";
                                break;
                            case 6:
                                threadData.strProcess2ThreadStatus = "...elastography...";
                                break;
                            case 7:
                                threadData.strProcess2ThreadStatus = "...spectroscopy...";
                                break;
                            case 8:
                                threadData.strProcess2ThreadStatus = "...spectral binning...";
                                break;
                        }   // switch (nProcess2Type

                        threadData.rwlsProcess1To2.ExitReadLock();

                        switch (nProcess2Type)
                        {
                            case 0:
                                threadData.strProcess2ThreadStatus = "...none...";
                                break;
                            case 1:
                                threadData.strProcess2ThreadStatus = "...intensity...";
                                break;
                            case 2:
                                threadData.strProcess2ThreadStatus = "...attenuation...";
                                break;
                            case 3:
                                threadData.strProcess2ThreadStatus = "...phase...";
                                break;
                            case 4:
                                threadData.strProcess2ThreadStatus = "...polarization...";
                                break;
                            case 5:
                                threadData.strProcess2ThreadStatus = "...angiography...";
                                break;
                            case 6:
                                threadData.strProcess2ThreadStatus = "...elastography...";
                                break;
                            case 7:
                                threadData.strProcess2ThreadStatus = "...spectroscopy...";
                                break;
                            case 8:
                                threadData.strProcess2ThreadStatus = "...spectral binning...";

                                // copy data to upper right data structures
                                // URImage
                                //for (nAline = 0; nAline < threadData.nProcessedNumberAlines; nAline++)
                                //    for (nPoint = 0; nPoint < threadData.nProcessedAlineLength; nPoint++)
                                //        UIData.pfURImage[nAline, nPoint] = (float)(Math.Sqrt(threadData.pfProcess2AIMAQParallel[nAline * threadData.nRawAlineLength + nPoint] * threadData.pfProcess2AIMAQParallel[nAline * threadData.nRawAlineLength + nPoint] + threadData.pfProcess2AIMAQPerpendicular[nAline * threadData.nRawAlineLength + nPoint] * threadData.pfProcess2AIMAQPerpendicular[nAline * threadData.nRawAlineLength + nPoint]));
                                //// URLeft
                                //nAline = UIData.nURLeft;
                                //if (nAline < 0) nAline = 0;
                                //if (nAline >= threadData.nProcessedNumberAlines) nAline = threadData.nProcessedNumberAlines - 1;
                                //for (nPoint = 0; nPoint < threadData.nProcessedAlineLength; nPoint++)
                                //    UIData.pfURLeft[0, nPoint] = UIData.pfURImage[nAline, nPoint];
                                //// URTop
                                //nPoint = UIData.nURTop;
                                //if (nPoint < 0) nPoint = 0;
                                //if (nPoint >= threadData.nProcessedAlineLength) nPoint = threadData.nProcessedAlineLength - 1;
                                //for (nAline = 0; nAline < threadData.nProcessedNumberAlines; nAline++)
                                //    UIData.pfURTop[0, nAline] = UIData.pfURImage[nAline, nPoint];

                                break;
                        }   // switch (nProcess2Type

                        threadData.strProcess2ThreadStatus = "done!";
                    }
                    else
                    {
                        bTroublemaker = true;
                        threadData.strProcess2ThreadStatus = "problem!";
                        threadData.mreProcess2Kill.Set();
                        threadData.mreMainKill.Set();
                    }

                }

            }  // if (WaitHandle.WaitAny
            #endregion

            #region cleanup
            if (bTroublemaker)
            {
                threadData.mreProcess2Dead.Set();
            }
            else
            {  // if (bTroublemaker
                #region cleanup
                threadData.strProcess2ThreadStatus = "Cleaning up...";
                // clean up code
                threadData.nProcess2Node = -1;
                // signal other threads
                threadData.mreProcess2Dead.Set();
                threadData.strProcess2ThreadStatus = "Done.";
                #endregion
            }  // if (bTroublemaker
            #endregion
        }


        void CleanupThread()
        {
#region initializing
            threadData.strCleanupThreadStatus = "Initializing...";

            // initialization
            bool bTroublemaker = false;
            LinkedListNode<CDataNode> nodeCleanup;
            nodeCleanup = nodeList.First;

            // set up wait handles to start
            WaitHandle[] pweStart = new WaitHandle[2];
            pweStart[0] = threadData.mreCleanupKill;
            pweStart[1] = threadData.mreCleanupRun;

            WaitHandle[] pweLoop = new WaitHandle[2];
            pweLoop[0] = threadData.mreCleanupAction;
            pweLoop[1] = threadData.mreCleanupKill;

            // initialization complete
            threadData.mreCleanupReady.Set();
            threadData.strCleanupThreadStatus = "Ready!";
#endregion

#region main loop
            threadData.strCleanupThreadStatus = "Set...";
            if (WaitHandle.WaitAny(pweStart) == 1)
            {
                threadData.strCleanupThreadStatus = "GO!";

                while (WaitHandle.WaitAny(pweLoop) == 0)
                {
                    threadData.strCleanupThreadStatus = "action...";
                    if (nodeCleanup.Value.rwls.TryEnterReadLock(1000) == true)
                    {
                        if (nodeCleanup.Value.nAcquired == 0)
                        {
                            threadData.strCleanupThreadStatus = "blank!";
                            nodeCleanup.Value.rwls.ExitReadLock();
                            threadData.mreCleanupAction.Reset();
                        }
                        else
                        {  // if (nodeCleanup.Value.nAcquired
                            if ((nodeCleanup.Value.nSaved > 0) && (nodeCleanup.Value.nProcessed > 0))
                            {
                                threadData.strCleanupThreadStatus = "cleaning!";
                                nodeCleanup.Value.nAcquired = 0;
                                nodeCleanup.Value.nSaved = 0;
                                nodeCleanup.Value.nProcessed = 0;
                                nodeCleanup.Value.rwls.ExitReadLock();
                                nodeCleanup = nodeCleanup.Next;
                                if (nodeCleanup == null)
                                    nodeCleanup = nodeList.First;
                            }
                            else
                            {  // if ((nodeCleanup.Value.nSaved
                                threadData.strCleanupThreadStatus = "waiting!";
                                nodeCleanup.Value.rwls.ExitReadLock();
                                threadData.mreCleanupAction.Reset();
                            }  // if ((nodeCleanup.Value.nSaved
                        }  // if (nodeCleanup.Value.nAcquired
                    }
                    else
                    {  // if (nodeCleanup.Value.rwls.TryEnterReadLock
                        threadData.strCleanupThreadStatus = "timeout!";
                        bTroublemaker = true;
                        threadData.mreCleanupKill.Set();
                        threadData.mreMainKill.Set();
                    }  // if (nodeCleanup.Value.rwls.TryEnterReadLock

                }  // while (WaitHandle.WaitAny
            }  // if (WaitHandle.WaitAny
#endregion

#region cleanup
            if (bTroublemaker)
            {
                threadData.mreCleanupDead.Set();
            }
            else
            {  // if (bTroublemaker
#region cleanup
                threadData.strCleanupThreadStatus = "Cleaning up...";
                // clean up code
                ;
                // signal other threads
                threadData.mreCleanupDead.Set();
                threadData.strCleanupThreadStatus = "Done.";
#endregion
            }  // if (bTroublemaker
#endregion

        }


        private void graphULMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point p = cursorULMain.GetRelativePosition();
            UIData.nULLeft = (int)(threadData.n1NumberCalibrationLines * threadData.n1NumberInterferenceLinesPerCalibration * p.X);
            UIData.nULTop = (int)(threadData.n1LineLength * (1 - p.Y));
        }


        private void graphURMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point p = cursorURMain.GetRelativePosition();
            UIData.nURLeft = (int)(threadData.n2NumberDepthProfilesPerSet * p.X);
            UIData.nURTop = (int)(threadData.n2LineLength * (1 - p.Y));
        }


        private void btnCalibrationFileLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                UIData.strCalibrationFile = openFileDialog.FileName;
                UIData.bCalibrationLoad = true;
                //StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                //lastParameter.WriteLine(openFileDialog.FileName);
                //lastParameter.Close();
            }   // if (openFileDialog.ShowDialog
        }


        private void btnCalibrationFileSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                UIData.strCalibrationFile = saveFileDialog.FileName;
                UIData.bCalibrationSave = true;
                //StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                //lastParameter.WriteLine(saveFileDialog.FileName);
                //lastParameter.Close();
            }   // if (saveFileDialog.ShowDialog
        }


        private void btnCalibrationClear_Click(object sender, RoutedEventArgs e)
        {
            UIData.bCalibrationClear = true;
        }


        private void btnFFTMaskUpdate_Click(object sender, RoutedEventArgs e)
        {
            UIData.bFFTMaskUpdated = true;
        }


        private void btnDispersionFileLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                UIData.strDispersionFile = openFileDialog.FileName;
                UIData.bDispersionLoad = true;
                //StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                //lastParameter.WriteLine(openFileDialog.FileName);
                //lastParameter.Close();
            }   // if (openFileDialog.ShowDialog
        }


        private void btnDispersionFileSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                UIData.strDispersionFile = saveFileDialog.FileName;
                UIData.bDispersionSave = true;
                //StreamWriter lastParameter = new StreamWriter("lastparameterfilename.txt");
                //lastParameter.WriteLine(saveFileDialog.FileName);
                //lastParameter.Close();
            }   // if (saveFileDialog.ShowDialog
        }


        private void btnDispersionClear_Click(object sender, RoutedEventArgs e)
        {
            UIData.bDispersionClear = true;
        }


    }



    public class CUIData : INotifyPropertyChanged
    {

        public Random rnd = new Random();
        public int nConfigState = 0;

        #region templates

        public string name_strXXX = "strXXX";
        private string _strXXX;
        public string strXXX
        {
            get { return _strXXX; }
            set { _strXXX = value; OnPropertyChanged(name_strXXX); }
        }   // public string strXXX

        public string name_nXXX = "nXXX";
        private int _nXXX;
        public int nXXX
        {
            get { return _nXXX; }
            set { _nXXX = value; OnPropertyChanged(name_nXXX); }
        }   // public int nXXX

        public string name_bXXX = "bXXX";
        private bool _bXXX;
        public bool bXXX
        {
            get { return _bXXX; }
            set { _bXXX = value; OnPropertyChanged(name_bXXX); }
        }   // public bool bXXX

        public string name_fXXX = "dXXX";
        private float _fXXX;
        public float fXXX
        {
            get { return _fXXX; }
            set { _fXXX = value; OnPropertyChanged(name_fXXX); }
        }   // public float fXXX

        #endregion templates

        #region UL

        public bool bULChange = false;

        #region general

        public string name_nULDisplayIndex = "nULDisplayIndex";
        private int _nULDisplayIndex;
        public int nULDisplayIndex
        {
            get { return _nULDisplayIndex; }
            set { _nULDisplayIndex = value; OnPropertyChanged(name_nULDisplayIndex); }
        }   // public int nULDisplayIndex

        #endregion general

        #region top

        public string name_bULTopActive = "bULTopActive";
        private bool _bULTopActive;
        public bool bULTopActive
        {
            get { return _bULTopActive; }
            set { _bULTopActive = value; OnPropertyChanged(name_bULTopActive); }
        }   // public bool bULTopActive

        public string name_nULTop = "nULTop";
        private int _nULTop;
        public int nULTop
        {
            get { return _nULTop; }
            set { _nULTop = value; OnPropertyChanged(name_nULTop); }
        }   // public int nULTop

        #endregion top

        #region left

        public string name_bULLeftActive = "bbULLeftActive";
        private bool _bULLeftActive;
        public bool bULLeftActive
        {
            get { return _bULLeftActive; }
            set { _bULLeftActive = value; OnPropertyChanged(name_bULLeftActive); }
        }   // public bool bULLeftActive

        public string name_nULLeft = "nULLeft";
        private int _nULLeft;
        public int nULLeft
        {
            get { return _nULLeft; }
            set { _nULLeft = value; OnPropertyChanged(name_nULLeft); }
        }   // public int nULLeft

        #endregion left

        #region DAQ

        public string name_nULDAQChannel = "nULDAQChannel";
        private int _nULDAQChannel;
        public int nULDAQChannel
        {
            get { return _nULDAQChannel; }
            set { _nULDAQChannel = value; OnPropertyChanged(name_nULDAQChannel); }
        }   // public int nULDAQChannel

        public string name_fULDAQMax = "fULDAQMax";
        private float _fULDAQMax;
        public float fULDAQMax
        {
            get { return _fULDAQMax; }
            set { _fULDAQMax = value; OnPropertyChanged(name_fULDAQMax); }
        }   // public float fULDAQMax

        public string name_fULDAQMin = "fULDAQMin";
        private float _fULDAQMin;
        public float fULDAQMin
        {
            get { return _fULDAQMin; }
            set { _fULDAQMin = value; OnPropertyChanged(name_fULDAQMin); }
        }   // public float fULDAQMin

        #endregion DAQ

        #region raw data

        public string name_bULRawDataShowCalibration = "bULRawDataShowCalibration";
        private bool _bULRawDataShowCalibration;
        public bool bULRawDataShowCalibration
        {
            get { return _bULRawDataShowCalibration; }
            set { _bULRawDataShowCalibration = value; OnPropertyChanged(name_bULRawDataShowCalibration); }
        }   // public bool bULRawDataShowCalibration

        public string name_fULRawDataMax = "fULRawDataMax";
        private float _fULRawDataMax;
        public float fULRawDataMax
        {
            get { return _fULRawDataMax; }
            set { _fULRawDataMax = value; OnPropertyChanged(name_fULRawDataMax); }
        }   // public float fULRawDataMax

        public string name_fULRawDataMin = "fULRawDataMin";
        private float _fULRawDataMin;
        public float fULRawDataMin
        {
            get { return _fULRawDataMin; }
            set { _fULRawDataMin = value; OnPropertyChanged(name_fULRawDataMin); }
        }   // public float fULRawDataMin

        #endregion raw data

        #region intensity

        public string name_nULIntensityChooser = "nULIntensityChooser";
        private int _nULIntensityChooser;
        public int nULIntensityChooser
        {
            get { return _nULIntensityChooser; }
            set { _nULIntensityChooser = value; OnPropertyChanged(name_nULIntensityChooser); }
        }   // public int nULIntensityChooser

        public string name_fULIntensityMax = "fULIntensityMax";
        private float _fULIntensityMax;
        public float fULIntensityMax
        {
            get { return _fULIntensityMax; }
            set { _fULIntensityMax = value; OnPropertyChanged(name_fULIntensityMax); }
        }   // public float fULIntensityMax

        public string name_fULIntensityMin = "fULIntensityMin";
        private float _fULIntensityMin;
        public float fULIntensityMin
        {
            get { return _fULIntensityMin; }
            set { _fULIntensityMin = value; OnPropertyChanged(name_fULIntensityMin); }
        }   // public float fULIntensityMin

        #endregion intensity

        #region main

        public string name_bULMainActive = "bULMainActive";
        private bool _bULMainActive;
        public bool bULMainActive
        {
            get { return _bULMainActive; }
            set { _bULMainActive = value; OnPropertyChanged(name_bULMainActive); }
        }   // public bool bULMainActive

        #endregion main

        #endregion  UL

        #region UR

        public bool bURChange = false;

        #region general

        public string name_nURDisplayIndex = "nURDisplayIndex";
        private int _nURDisplayIndex;
        public int nURDisplayIndex
        {
            get { return _nURDisplayIndex; }
            set { _nURDisplayIndex = value; OnPropertyChanged(name_nURDisplayIndex); }
        }   // public int nURDisplayIndex

        #endregion general

        #region top

        public string name_bURTopActive = "bURTopActive";
        private bool _bURTopActive;
        public bool bURTopActive
        {
            get { return _bURTopActive; }
            set { _bURTopActive = value; OnPropertyChanged(name_bURTopActive); }
        }   // public bool bURTopActive

        public string name_nURTop = "nURTop";
        private int _nURTop;
        public int nURTop
        {
            get { return _nURTop; }
            set { _nURTop = value; OnPropertyChanged(name_nURTop); }
        }   // public int nURTop

        #endregion top

        #region left

        public string name_bURLeftActive = "bbURLeftActive";
        private bool _bURLeftActive;
        public bool bURLeftActive
        {
            get { return _bURLeftActive; }
            set { _bURLeftActive = value; OnPropertyChanged(name_bURLeftActive); }
        }   // public bool bURLeftActive

        public string name_nURLeft = "nURLeft";
        private int _nURLeft;
        public int nURLeft
        {
            get { return _nURLeft; }
            set { _nURLeft = value; OnPropertyChanged(name_nURLeft); }
        }   // public int nURLeft

        #endregion left

        #region main

        public string name_bURMainActive = "bURMainActive";
        private bool _bURMainActive;
        public bool bURMainActive
        {
            get { return _bURMainActive; }
            set { _bURMainActive = value; OnPropertyChanged(name_bURMainActive); }
        }   // public bool bURMainActive

        #endregion main

        #region intensity

        public string name_nURIntensityCamera = "nURIntensityCamera";
        private int _nURIntensityCamera;
        public int nURIntensityCamera
        {
            get { return _nURIntensityCamera; }
            set { _nURIntensityCamera = value; OnPropertyChanged(name_nURIntensityCamera); }
        }   // public int nURIntensityCamera

        public string name_nURIntensityLines = "nURIntensityLines";
        private int _nURIntensityLines;
        public int nURIntensityLines
        {
            get { return _nURIntensityLines; }
            set { _nURIntensityLines = value; OnPropertyChanged(name_nURIntensityLines); }
        }   // public int nURIntensityLines

        public string name_fURIntensityMax = "fURIntensityMax";
        private float _fURIntensityMax;
        public float fURIntensityMax
        {
            get { return _fURIntensityMax; }
            set { _fURIntensityMax = value; OnPropertyChanged(name_fURIntensityMax); }
        }   // public float fURIntensityMax

        public string name_fURIntensityMin = "fURIntensityMin";
        private float _fURIntensityMin;
        public float fURIntensityMin
        {
            get { return _fURIntensityMin; }
            set { _fURIntensityMin = value; OnPropertyChanged(name_fURIntensityMin); }
        }   // public float fURIntensityMin

        #endregion intensity

        #endregion UR

        #region LL

        public string name_strLLConfigurationFilename = "strLLConfigurationFilename";
        private string _strLLConfigurationFilename;
        public string strLLConfigurationFilename
        {
            get { return _strLLConfigurationFilename; }
            set { _strLLConfigurationFilename = value; OnPropertyChanged(name_strLLConfigurationFilename); }
        }   // public string strLLConfigurationFilename

        public string name_strLLAlazarBoard = "strLLAlazarBoard";
        private string _strLLAlazarBoard;
        public string strLLAlazarBoard
        {
            get { return _strLLAlazarBoard; }
            set { _strLLAlazarBoard = value; OnPropertyChanged(name_strLLAlazarBoard); }
        }   // public string strLLAlazarBoard

        public string name_bLLAlazarCh1 = "bLLAlazarCh1";
        private bool _bLLAlazarCh1;
        public bool bLLAlazarCh1
        {
            get { return _bLLAlazarCh1; }
            set { _bLLAlazarCh1 = value; OnPropertyChanged(name_bLLAlazarCh1); }
        }   // public bool bLLAlazarCh1

        public string name_bLLAlazarCh2 = "bLLAlazarCh2";
        private bool _bLLAlazarCh2;
        public bool bLLAlazarCh2
        {
            get { return _bLLAlazarCh2; }
            set { _bLLAlazarCh2 = value; OnPropertyChanged(name_bLLAlazarCh2); }
        }   // public bool bLLAlazarCh2

        public string name_nLLAlazarLineLength = "nLLAlazarLineLength";
        private int _nLLAlazarLineLength;
        public int nLLAlazarLineLength
        {
            get { return _nLLAlazarLineLength; }
            set { _nLLAlazarLineLength = value; OnPropertyChanged(name_nLLAlazarLineLength); }
        }   // public int nLLAlazarLineLength

        public string name_nLLAlazarSamplingRate = "nLLAlazarSamplingRate";
        private int _nLLAlazarSamplingRate;
        public int nLLAlazarSamplingRate
        {
            get { return _nLLAlazarSamplingRate; }
            set { _nLLAlazarSamplingRate = value; OnPropertyChanged(name_nLLAlazarSamplingRate); }
        }   // public int nLLAlazarSamplingRate

        public string name_strLLDAQDevice = "strLLDAQDevice";
        private string _strLLDAQDevice;
        public string strLLDAQDevice
        {
            get { return _strLLDAQDevice; }
            set { _strLLDAQDevice = value; OnPropertyChanged(name_strLLDAQDevice); }
        }   // public string strLLDAQDevice

        public string name_strLLIMAQParallel = "strLLIMAQParallel";
        private string _strLLIMAQParallel;
        public string strLLIMAQParallel
        {
            get { return _strLLIMAQParallel; }
            set { _strLLIMAQParallel = value; OnPropertyChanged(name_strLLIMAQParallel); }
        }   // public string strLLIMAQParallel

        public string name_strLLIMAQPerpendicular = "strLLIMAQPerpendicular";
        private string _strLLIMAQPerpendicular;
        public string strLLIMAQPerpendicular
        {
            get { return _strLLIMAQPerpendicular; }
            set { _strLLIMAQPerpendicular = value; OnPropertyChanged(name_strLLIMAQPerpendicular); }
        }   // public string strLLIMAQPerpendicular

        public string name_nLLIMAQLineLength = "nLLIMAQLineLength";
        private int _nLLIMAQLineLength;
        public int nLLIMAQLineLength
        {
            get { return _nLLIMAQLineLength; }
            set { _nLLIMAQLineLength = value; OnPropertyChanged(name_nLLIMAQLineLength); }
        }   // public int nLLIMAQLineLength

        public string name_nLLIMAQRingBuffers = "nLLIMAQRingBuffers";
        private int _nLLIMAQRingBuffers;
        public int nLLIMAQRingBuffers
        {
            get { return _nLLIMAQRingBuffers; }
            set { _nLLIMAQRingBuffers = value; OnPropertyChanged(name_nLLIMAQRingBuffers); }
        }   // public int nLLIMAQRingBuffers

        public string name_nLLSystemType = "nLLSystemType";
        private int _nLLSystemType;
        public int nLLSystemType
        {
            get { return _nLLSystemType; }
            set { _nLLSystemType = value; OnPropertyChanged(name_nLLSystemType); }
        }   // public int nLLSystemType

        public string name_nLLSystemActual = "nLLSystemActual";
        private int _nLLSystemActual;
        public int nLLSystemActual
        {
            get { return _nLLSystemActual; }
            set { _nLLSystemActual = value; OnPropertyChanged(name_nLLSystemActual); }
        }   // public int nLLSystemActual

        public string name_nLLLineRate = "nLLLineRate";
        private int _nLLLineRate;
        public int nLLLineRate
        {
            get { return _nLLLineRate; }
            set { _nLLLineRate = value; OnPropertyChanged(name_nLLLineRate); }
        }   // public int nLLLineRate

        public string name_nLLLinesPerChunk = "nLLLinesPerChunk";
        private int _nLLLinesPerChunk;
        public int nLLLinesPerChunk
        {
            get { return _nLLLinesPerChunk; }
            set { _nLLLinesPerChunk = value; OnPropertyChanged(name_nLLLinesPerChunk); }
        }   // public int nLLLinesPerChunk

        public string name_nLLChunksPerImage = "nLLChunksPerImage";
        private int _nLLChunksPerImage;
        public int nLLChunksPerImage
        {
            get { return _nLLChunksPerImage; }
            set { _nLLChunksPerImage = value; OnPropertyChanged(name_nLLChunksPerImage); }
        }   // public int nLLChunksPerImage

        public string name_nLLImagesPerVolume = "nLLImagesPerVolume";
        private int _nLLImagesPerVolume;
        public int nLLImagesPerVolume
        {
            get { return _nLLImagesPerVolume; }
            set { _nLLImagesPerVolume = value; OnPropertyChanged(name_nLLImagesPerVolume); }
        }   // public int nLLImagesPerVolume

        public string name_nLLLinkedListLength = "nLLLinkedListLength";
        private int _nLLLinkedListLength;
        public int nLLLinkedListLength
        {
            get { return _nLLLinkedListLength; }
            set { _nLLLinkedListLength = value; OnPropertyChanged(name_nLLLinkedListLength); }
        }   // public int nLLLinkedListLength

        public string name_nComputationDevice = "nComputationDevice";
        private int _nComputationDevice;
        public int nComputationDevice
        {
            get { return _nComputationDevice; }
            set { _nComputationDevice = value; OnPropertyChanged(name_nComputationDevice); }
        }   // public int nComputationDevice

        public string name_strLLFileDirectory = "strLLFileDirectory";
        private string _strLLFileDirectory;
        public string strLLFileDirectory
        {
            get { return _strLLFileDirectory; }
            set { _strLLFileDirectory = value; OnPropertyChanged(name_strLLFileDirectory); }
        }   // public string strLLFileDirectory

        public string name_strLLFilePrefix = "strLLFilePrefix";
        private string _strLLFilePrefix;
        public string strLLFilePrefix
        {
            get { return _strLLFilePrefix; }
            set { _strLLFilePrefix = value; OnPropertyChanged(name_strLLFilePrefix); }
        }   // public string strLLFilePrefix

        public string name_nLLFileNumber = "nLLFileNumber";
        private int _nLLFileNumber;
        public int nLLFileNumber
        {
            get { return _nLLFileNumber; }
            set { _nLLFileNumber = value; OnPropertyChanged(name_nLLFileNumber); }
        }   // public int nLLFileNumber

        public string name_bLLFileRecord = "bLLFileRecord";
        private bool _bLLFileRecord;
        public bool bLLFileRecord
        {
            get { return _bLLFileRecord; }
            set { _bLLFileRecord = value; OnPropertyChanged(name_bLLFileRecord); }
        }   // public bool bLLFileRecord

        public string name_nLLFileCycle = "nLLFileCycle";
        private int _nLLFileCycle;
        public int nLLFileCycle
        {
            get { return _nLLFileCycle; }
            set { _nLLFileCycle = value; OnPropertyChanged(name_nLLFileCycle); }
        }   // public int nLLFileCycle

        public string name_fLLCenterX = "fLLCenterX";
        private float _fLLCenterX;
        public float fLLCenterX
        {
            get { return _fLLCenterX; }
            set { _fLLCenterX = value; OnPropertyChanged(name_fLLCenterX); }
        }   // public float fLLCenterX

        public string name_fLLCenterY = "fLLCenterY";
        private float _fLLCenterY;
        public float fLLCenterY
        {
            get { return _fLLCenterY; }
            set { _fLLCenterY = value; OnPropertyChanged(name_fLLCenterY); }
        }   // public float fLLCenterY

        public string name_fLLFastAngle = "fLLFastAngle";
        private float _fLLFastAngle;
        public float fLLFastAngle
        {
            get { return _fLLFastAngle; }
            set { _fLLFastAngle = value; OnPropertyChanged(name_fLLFastAngle); }
        }   // public float fLLFastAngle

        public string name_fLLRangeFast = "fLLRangeFast";
        private float _fLLRangeFast;
        public float fLLRangeFast
        {
            get { return _fLLRangeFast; }
            set { _fLLRangeFast = value; OnPropertyChanged(name_fLLRangeFast); }
        }   // public float fLLRangeFast

        public string name_fLLRangeSlow = "fLLRangeSlow";
        private float _fLLRangeSlow;
        public float fLLRangeSlow
        {
            get { return _fLLRangeSlow; }
            set { _fLLRangeSlow = value; OnPropertyChanged(name_fLLRangeSlow); }
        }   // public float fLLRangeSlow

        public string name_nLLDwellFast = "nLLDwellFast";
        private int _nLLDwellFast;
        public int nLLDwellFast
        {
            get { return _nLLDwellFast; }
            set { _nLLDwellFast = value; OnPropertyChanged(name_nLLDwellFast); }
        }   // public int nLLDwellFast

        public string name_nLLDwellSlow = "nLLDwellSlow";
        private int _nLLDwellSlow;
        public int nLLDwellSlow
        {
            get { return _nLLDwellSlow; }
            set { _nLLDwellSlow = value; OnPropertyChanged(name_nLLDwellSlow); }
        }   // public int nLLDwellSlow

        public string name_nLLRounding = "nLLRounding";
        private int _nLLRounding;
        public int nLLRounding
        {
            get { return _nLLRounding; }
            set { _nLLRounding = value; OnPropertyChanged(name_nLLRounding); }
        }   // public int nLLRounding

        public string name_fLLPolModLow = "fLLPolModLow";
        private float _fLLPolModLow;
        public float fLLPolModLow
        {
            get { return _fLLPolModLow; }
            set { _fLLPolModLow = value; OnPropertyChanged(name_fLLPolModLow); }
        }   // public float fLLPolModLow

        public string name_fLLPolModHigh = "fLLPolModHigh";
        private float _fLLPolModHigh;
        public float fLLPolModHigh
        {
            get { return _fLLPolModHigh; }
            set { _fLLPolModHigh = value; OnPropertyChanged(name_fLLPolModHigh); }
        }   // public float fLLPolModHigh


        #endregion

        #region LR

        #region diagnostics tab

        public string name_fLRAvailableMemory = "fLRAvailableMemory";
        private float _fLRAvailableMemory;
        public float fLRAvailableMemory
        {
            get { return _fLRAvailableMemory; }
            set { _fLRAvailableMemory = value; OnPropertyChanged(name_fLRAvailableMemory); }
        }   // public float fLRAvailableMemory

        public string name_fLRUIUpdateTime = "fLRUIUpdateTime";
        private float _fLRUIUpdateTime;
        public float fLRUIUpdateTime
        {
            get { return _fLRUIUpdateTime; }
            set { _fLRUIUpdateTime = value; OnPropertyChanged(name_fLRUIUpdateTime); }
        }   // public float fLRUIUpdateTime

        public string name_strLRMainThreadStatus = "strLRMainThreadStatus";
        private string _strLRMainThreadStatus;
        public string strLRMainThreadStatus
        {
            get { return _strLRMainThreadStatus; }
            set { _strLRMainThreadStatus = value; OnPropertyChanged(name_strLRMainThreadStatus); }
        }   // public string strLRMainThreadStatus

        public string name_strLROutputThreadStatus = "strLROutputThreadStatus";
        private string _strLROutputThreadStatus;
        public string strLROutputThreadStatus
        {
            get { return _strLROutputThreadStatus; }
            set { _strLROutputThreadStatus = value; OnPropertyChanged(name_strLROutputThreadStatus); }
        }   // public string strLROutputThreadStatus

        public string name_strLRCleanupThreadStatus = "strLRCleanupThreadStatus";
        private string _strLRCleanupThreadStatus;
        public string strLRCleanupThreadStatus
        {
            get { return _strLRCleanupThreadStatus; }
            set { _strLRCleanupThreadStatus = value; OnPropertyChanged(name_strLRCleanupThreadStatus); }
        }   // public string strLRCleanupThreadStatus

        public string name_strLRSaveThreadStatus = "strLRSaveThreadStatus";
        private string _strLRSaveThreadStatus;
        public string strLRSaveThreadStatus
        {
            get { return _strLRSaveThreadStatus; }
            set { _strLRSaveThreadStatus = value; OnPropertyChanged(name_strLRSaveThreadStatus); }
        }   // public string strLRSaveThreadStatus

        public string name_strLRAcquireThreadStatus = "strLRAcquireThreadStatus";
        private string _strLRAcquireThreadStatus;
        public string strLRAcquireThreadStatus
        {
            get { return _strLRAcquireThreadStatus; }
            set { _strLRAcquireThreadStatus = value; OnPropertyChanged(name_strLRAcquireThreadStatus); }
        }   // public string strLRAcquireThreadStatus

        public string name_strLRProcessThreadStatus = "strLRProcessThreadStatus";
        private string _strLRProcessThreadStatus;
        public string strLRProcessThreadStatus
        {
            get { return _strLRProcessThreadStatus; }
            set { _strLRProcessThreadStatus = value; OnPropertyChanged(name_strLRProcessThreadStatus); }
        }   // public string strLRProcessThreadStatus

        public string name_strLRProcess1ThreadStatus = "strLRProcess1ThreadStatus";
        private string _strLRProcess1ThreadStatus;
        public string strLRProcess1ThreadStatus
        {
            get { return _strLRProcess1ThreadStatus; }
            set { _strLRProcess1ThreadStatus = value; OnPropertyChanged(name_strLRProcess1ThreadStatus); }
        }   // public string strLRProcess1ThreadStatus

        public string name_strLRProcess2ThreadStatus = "strLRProcess2ThreadStatus";
        private string _strLRProcess2ThreadStatus;
        public string strLRProcess2ThreadStatus
        {
            get { return _strLRProcess2ThreadStatus; }
            set { _strLRProcess2ThreadStatus = value; OnPropertyChanged(name_strLRProcess2ThreadStatus); }
        }   // public string strLRProcess2ThreadStatus

        public string name_bLRDiagnostics = "bLRDiagnostics";
        private bool _bLRDiagnostics;
        public bool bLRDiagnostics
        {
            get { return _bLRDiagnostics; }
            set { _bLRDiagnostics = value; OnPropertyChanged(name_bLRDiagnostics); }
        }   // public bool bLRDiagnostics

        #endregion  // diagnostics tab

        #region output tab

        public string name_bLROutput = "bLROutput";
        private bool _bLROutput;
        public bool bLROutput
        {
            get { return _bLROutput; }
            set { _bLROutput = value; OnPropertyChanged(name_bLROutput); }
        }   // public bool bLROutput

        public string name_nOutputDigitalClockType = "nOutputDigitalClockType";
        private int _nOutputDigitalClockType;
        public int nOutputDigitalClockType
        {
            get { return _nOutputDigitalClockType; }
            set { _nOutputDigitalClockType = value; OnPropertyChanged(name_nOutputDigitalClockType); }
        }   // public int nOutputDigitalClockType

        public string name_nOutputP0L0Type = "nOutputP0L0Type";
        private int _nOutputP0L0Type;
        public int nOutputP0L0Type
        {
            get { return _nOutputP0L0Type; }
            set { _nOutputP0L0Type = value; OnPropertyChanged(name_nOutputP0L0Type); }
        }   // public int nOutputP0L0Type

        public string name_nOutputP0L1Type = "nOutputP0L1Type";
        private int _nOutputP0L1Type;
        public int nOutputP0L1Type
        {
            get { return _nOutputP0L1Type; }
            set { _nOutputP0L1Type = value; OnPropertyChanged(name_nOutputP0L1Type); }
        }   // public int nOutputP0L1Type

        public string name_nOutputP0L2Type = "nOutputP0L2Type";
        private int _nOutputP0L2Type;
        public int nOutputP0L2Type
        {
            get { return _nOutputP0L2Type; }
            set { _nOutputP0L2Type = value; OnPropertyChanged(name_nOutputP0L2Type); }
        }   // public int nOutputP0L2Type

        public string name_nOutputAnalogClockType = "nOutputAnalogClockType";
        private int _nOutputAnalogClockType;
        public int nOutputAnalogClockType
        {
            get { return _nOutputAnalogClockType; }
            set { _nOutputAnalogClockType = value; OnPropertyChanged(name_nOutputAnalogClockType); }
        }   // public int nOutputAnalogClockType

        public string name_nOutputAO0Type = "nOutputAO0Type";
        private int _nOutputAO0Type;
        public int nOutputAO0Type
        {
            get { return _nOutputAO0Type; }
            set { _nOutputAO0Type = value; OnPropertyChanged(name_nOutputAO0Type); }
        }   // public int nOutputAO0Type

        public string name_nOutputAO1Type = "nOutputAO1Type";
        private int _nOutputAO1Type;
        public int nOutputAO1Type
        {
            get { return _nOutputAO1Type; }
            set { _nOutputAO1Type = value; OnPropertyChanged(name_nOutputAO1Type); }
        }   // public int nOutputAO1Type

        public string name_nOutputAO2Type = "nOutputAO2Type";
        private int _nOutputAO2Type;
        public int nOutputAO2Type
        {
            get { return _nOutputAO2Type; }
            set { _nOutputAO2Type = value; OnPropertyChanged(name_nOutputAO2Type); }
        }   // public int nOutputAO2Type

        #endregion output tab

        #region processing tab

        public string name_bLRReferenceActive = "bLRReferenceActive";
        private bool _bLRReferenceActive;
        public bool bLRReferenceActive
        {
            get { return _bLRReferenceActive; }
            set { _bLRReferenceActive = value; OnPropertyChanged(name_bLRReferenceActive); }
        }   // public bool bLRReferenceActive

        public string name_nLRReferenceMethod = "nLRReferenceMethod";
        private int _nLRReferenceMethod;
        public int nLRReferenceMethod
        {
            get { return _nLRReferenceMethod; }
            set { _nLRReferenceMethod = value; OnPropertyChanged(name_nLRReferenceMethod); }
        }   // public int nLRReferenceMethod

        public string name_nLRReferenceDisplay = "nLRReferenceDisplay";
        private int _nLRReferenceDisplay;
        public int nLRReferenceDisplay
        {
            get { return _nLRReferenceDisplay; }
            set { _nLRReferenceDisplay = value; OnPropertyChanged(name_nLRReferenceDisplay); }
        }   // public int nLRReferenceDisplay

        public string name_nFFTMaskLeft = "nFFTMaskLeft";
        private int _nFFTMaskLeft;
        public int nFFTMaskLeft
        {
            get { return _nFFTMaskLeft; }
            set { _nFFTMaskLeft = value; OnPropertyChanged(name_nFFTMaskLeft); }
        }   // public int nFFTMaskLeft

        public string name_nFFTMaskRight = "nFFTMaskRight";
        private int _nFFTMaskRight;
        public int nFFTMaskRight
        {
            get { return _nFFTMaskRight; }
            set { _nFFTMaskRight = value; OnPropertyChanged(name_nFFTMaskRight); }
        }   // public int nFFTMaskRight

        public string name_nFFTMaskRound = "nFFTMaskRound";
        private int _nFFTMaskRound;
        public int nFFTMaskRound
        {
            get { return _nFFTMaskRound; }
            set { _nFFTMaskRound = value; OnPropertyChanged(name_nFFTMaskRound); }
        }   // public int nFFTMaskRound

        public bool bFFTMaskUpdated = false;

        #endregion  // processing tab

        #region calibration tab

        public string name_strCalibrationFile = "strCalibrationFile";
        private string _strCalibrationFile;
        public string strCalibrationFile
        {
            get { return _strCalibrationFile; }
            set { _strCalibrationFile = value; OnPropertyChanged(name_strCalibrationFile); }
        }   // public string strCalibrationFile

        public string name_bCalibrationActive = "bCalibrationActive";
        private bool _bCalibrationActive;
        public bool bCalibrationActive
        {
            get { return _bCalibrationActive; }
            set { _bCalibrationActive = value; OnPropertyChanged(name_bCalibrationActive); }
        }   // public bool bCalibrationActive

        public string name_nCalibrationDepthLeft = "nCalibrationDepthLeft";
        private int _nCalibrationDepthLeft;
        public int nCalibrationDepthLeft
        {
            get { return _nCalibrationDepthLeft; }
            set { _nCalibrationDepthLeft = value; OnPropertyChanged(name_nCalibrationDepthLeft); }
        }   // public int nCalibrationDepthLeft

        public string name_nCalibrationDepthRight = "nCalibrationDepthRight";
        private int _nCalibrationDepthRight;
        public int nCalibrationDepthRight
        {
            get { return _nCalibrationDepthRight; }
            set { _nCalibrationDepthRight = value; OnPropertyChanged(name_nCalibrationDepthRight); }
        }   // public int nCalibrationDepthRight

        public string name_nCalibrationDepthRound = "nCalibrationDepthRound";
        private int _nCalibrationDepthRound;
        public int nCalibrationDepthRound
        {
            get { return _nCalibrationDepthRound; }
            set { _nCalibrationDepthRound = value; OnPropertyChanged(name_nCalibrationDepthRound); }
        }   // public int nCalibrationDepthRound

        public string name_nCalibrationPhaseLeft = "nCalibrationPhaseLeft";
        private int _nCalibrationPhaseLeft;
        public int nCalibrationPhaseLeft
        {
            get { return _nCalibrationPhaseLeft; }
            set { _nCalibrationPhaseLeft = value; OnPropertyChanged(name_nCalibrationPhaseLeft); }
        }   // public int nCalibrationPhaseLeft

        public string name_nCalibrationPhaseRight = "nCalibrationPhaseRight";
        private int _nCalibrationPhaseRight;
        public int nCalibrationPhaseRight
        {
            get { return _nCalibrationPhaseRight; }
            set { _nCalibrationPhaseRight = value; OnPropertyChanged(name_nCalibrationPhaseRight); }
        }   // public int nCalibrationPhaseRight

        public bool bCalibrationLoad;
        public bool bCalibrationSave;
        public bool bCalibrationClear;

        #endregion  // calibration tab

        #region dispersion tab

        public string name_strDispersionFile = "strDispersionFile";
        private string _strDispersionFile;
        public string strDispersionFile
        {
            get { return _strDispersionFile; }
            set { _strDispersionFile = value; OnPropertyChanged(name_strDispersionFile); }
        }   // public string strDispersionFile

        public string name_bDispersionActive = "bDispersionActive";
        private bool _bDispersionActive;
        public bool bDispersionActive
        {
            get { return _bDispersionActive; }
            set { _bDispersionActive = value; OnPropertyChanged(name_bDispersionActive); }
        }   // public bool bDispersionActive

        public string name_nDispersionLine = "nDispersionLine";
        private int _nDispersionLine;
        public int nDispersionLine
        {
            get { return _nDispersionLine; }
            set { _nDispersionLine = value; OnPropertyChanged(name_nDispersionLine); }
        }   // public int nDispersionLine

        public string name_nDispersionDepthLeft = "nDispersionDepthLeft";
        private int _nDispersionDepthLeft;
        public int nDispersionDepthLeft
        {
            get { return _nDispersionDepthLeft; }
            set { _nDispersionDepthLeft = value; OnPropertyChanged(name_nDispersionDepthLeft); }
        }   // public int nDispersionDepthLeft

        public string name_nDispersionDepthRight = "nDispersionDepthRight";
        private int _nDispersionDepthRight;
        public int nDispersionDepthRight
        {
            get { return _nDispersionDepthRight; }
            set { _nDispersionDepthRight = value; OnPropertyChanged(name_nDispersionDepthRight); }
        }   // public int nDispersionDepthRight

        public string name_nDispersionDepthRound = "nDispersionDepthRound";
        private int _nDispersionDepthRound;
        public int nDispersionDepthRound
        {
            get { return _nDispersionDepthRound; }
            set { _nDispersionDepthRound = value; OnPropertyChanged(name_nDispersionDepthRound); }
        }   // public int nDispersionDepthRound

        public string name_nDispersionPhaseLeft = "nDispersionPhaseLeft";
        private int _nDispersionPhaseLeft;
        public int nDispersionPhaseLeft
        {
            get { return _nDispersionPhaseLeft; }
            set { _nDispersionPhaseLeft = value; OnPropertyChanged(name_nDispersionPhaseLeft); }
        }   // public int nDispersionPhaseLeft

        public string name_nDispersionPhaseRight = "nDispersionPhaseRight";
        private int _nDispersionPhaseRight;
        public int nDispersionPhaseRight
        {
            get { return _nDispersionPhaseRight; }
            set { _nDispersionPhaseRight = value; OnPropertyChanged(name_nDispersionPhaseRight); }
        }   // public int nDispersionPhaseRight

        public bool bDispersionLoad;
        public bool bDispersionSave;
        public bool bDispersionClear;

        #endregion  // dispersion tab

        #endregion  // LR

        #region graphs

        public float[,] pfULLeft = null;
        public float[,] pfULTop = null;
        public float[,] pfULImage = null;

        public float[,] pfURLeft = null;
        public float[,] pfURTop = null;
        public float[,] pfURImage = null;

        public int[,] pnLinkedList = null;

        public float[,] pfOutput = null;

        public float[,] pfReference = null;

        public float[,] pfCalibrationDepthProfile = null;
        public float[,] pfCalibrationSpectrum = null;
        public float[,] pfCalibrationPhase = null;

        public float[,] pfDispersionDepthProfile = null;
        public float[,] pfDispersionSpectrum = null;
        public float[,] pfDispersionPhase = null;

        #endregion  // graphs


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }   // if (handler
        }   // protected void OnPropertyChanged

    }   // public class CUIData


    public class CDataNode
    {

        public int nNodeID;
        /* Begin: 20201208 editing by JL */
        public string strFilename;
        public int nFileNumber; 
        public int nFramePosition;
        /* End: 20201208 editing by JL */

        public ulong nSize;
        public ReaderWriterLockSlim rwls;
        public int nAcquired;
        public bool bRecord;
        public int nSaved;
        public int nProcessed;

        public UInt16[][] pnAlazar;
        public float[] pfDAQ;
        /* Begin: 20201208 editing by JL */
        // SD-OCT
        public Int16[][] pnIMAQ; 
        // PS-SD-OCT
        public Int16[][] pnIMAQParallel;
        public Int16[][] pnIMAQPerpendicular;
        /* End: 20201208 editing by JL */

        public CDataNode(CUIData uiData, int nodeID)
        {
            nSize = sizeof(ulong);
            nNodeID = nodeID; nSize += sizeof(int);

            rwls = new ReaderWriterLockSlim(); nSize += 0;
            nAcquired = 0; nSize += sizeof(int);
            nSaved = 0; nSize += sizeof(int);
            nProcessed = 0; nSize += sizeof(int);

            int nNumberChunks, nLinesPerChunk, nLineLength, nChannels;

            switch (uiData.nLLSystemType)
            {
                case 0: // SD-OCT
                    nNumberChunks = uiData.nLLChunksPerImage;
                    nLinesPerChunk = uiData.nLLLinesPerChunk;
                    nLineLength = uiData.nLLIMAQLineLength;
                    pnIMAQParallel = new Int16[nNumberChunks][];
                    pnIMAQPerpendicular = new Int16[nNumberChunks][];
                    for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                    {
                        pnIMAQParallel[nChunk] = new Int16[nLinesPerChunk * nLineLength];
                        nSize += Convert.ToUInt64(2 * nLinesPerChunk * nLineLength) * sizeof(Int16);
                        Array.Clear(pnIMAQParallel[nChunk], 0, pnIMAQParallel[nChunk].Length);
                    }   // for (int nChunk
                    pfDAQ = new float[4 * nNumberChunks * nLinesPerChunk];
                    nSize += Convert.ToUInt64(4 * nNumberChunks * nLinesPerChunk * sizeof(float));
                    Array.Clear(pfDAQ, 0, pfDAQ.Length);
                    break;
                case 1: // PS-SD-OCT
                    nNumberChunks = uiData.nLLChunksPerImage;
                    nLinesPerChunk = uiData.nLLLinesPerChunk;
                    nLineLength = uiData.nLLIMAQLineLength;
                    pnIMAQParallel = new Int16[nNumberChunks][];
                    pnIMAQPerpendicular = new Int16[nNumberChunks][];
                    for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                    {
                        pnIMAQParallel[nChunk] = new Int16[nLinesPerChunk * nLineLength];
                        pnIMAQPerpendicular[nChunk] = new Int16[nLinesPerChunk * nLineLength];
                        nSize += Convert.ToUInt64(2 * nLinesPerChunk * nLineLength) * sizeof(Int16);
                        Array.Clear(pnIMAQParallel[nChunk], 0, pnIMAQParallel[nChunk].Length);
                        Array.Clear(pnIMAQPerpendicular[nChunk], 0, pnIMAQParallel[nChunk].Length);
                    }   // for (int nChunk
                    pfDAQ = new float[4 * nNumberChunks * nLinesPerChunk];
                    nSize += Convert.ToUInt64(4 * nNumberChunks * nLinesPerChunk * sizeof(float));
                    Array.Clear(pfDAQ, 0, pfDAQ.Length);
                    break;
                case 2: // line field

                    break;
                case 3: // OFDI (pgreg002 here is a section to see how the arrays are defined in each node
                    nNumberChunks = uiData.nLLChunksPerImage;
                    nLinesPerChunk = uiData.nLLLinesPerChunk;
                    nLineLength = uiData.nLLAlazarLineLength;
                    nChannels = 0;
                    if (uiData.bLLAlazarCh1 == true)
                        nChannels++;
                    if (uiData.bLLAlazarCh2 == true)
                        nChannels++;
                    pnAlazar = new UInt16[nNumberChunks][];
                    for (int nChunk = 0; nChunk < nNumberChunks; nChunk++)
                    {
                        pnAlazar[nChunk] = new UInt16[nChannels * nLinesPerChunk * nLineLength];  // 2 - MZI + OCT
                        nSize += Convert.ToUInt64(nChannels * nLinesPerChunk * nLineLength) * sizeof(UInt16);
                        Array.Clear(pnAlazar[nChunk], 0, pnAlazar[nChunk].Length);
                    }   // for (int nChunk
                    pfDAQ = new float[4 * nNumberChunks * nLinesPerChunk];
                    nSize += Convert.ToUInt64(4 * nNumberChunks * nLinesPerChunk * sizeof(float));
                    Array.Clear(pfDAQ, 0, pfDAQ.Length);
                    break;
                case 4: // PS-OFDI

                    break;
            }
        }   // public CDataNode

        ~CDataNode()
        {
            rwls.Dispose();
        }   // ~CDataNode

    }   // public class CDataNode


    public class CThreadData
    {
//        public int nRawNumberAlines;
//        public int nRawAlineLength;
//        public int nProcessedNumberAlines;
//        public int nProcessedAlineLength;

        public bool bRecord = false;

        #region MainThread
        public Thread threadMain;
        public ManualResetEvent mreMainReady;
        public ManualResetEvent mreMainRun;
        public ManualResetEvent mreMainKill;
        public ManualResetEvent mreMainDead;
        public string strMainThreadStatus = "XXX";
        #endregion

        #region OutputThread
        public Thread threadOutput;
        public ManualResetEvent mreOutputReady;
        public ManualResetEvent mreOutputRun;
        public ManualResetEvent mreOutputKill;
        public ManualResetEvent mreOutputDead;
        public ManualResetEvent mreOutputUpdate;
        public string strOutputThreadStatus = "XXX";
        #endregion

        #region AcquireThread
        public Thread threadAcquire;
        public ManualResetEvent mreAcquireReady;
        public ManualResetEvent mreAcquireRun;
        public ManualResetEvent mreAcquireKill;
        public ManualResetEvent mreAcquireDead;
        public SemaphoreSlim ssAcquireComplete;
        public string strAcquireThreadStatus = "XXX";
        /* Begin: 20201208 editing JL */
        public int nAcquisitionNodeID;
        public int nFileNumber = 100001; 
        public int nFramePosition = 1;
        /* End: 20201208 editing JL */
        #endregion

        public int nSystemActual;
        public LinkedListNode<CDataNode> nodeAcquire;
        public ManualResetEvent mreAcquireNodeReady;

        #region AcquireAlazarThread
        public Thread threadAcquireAlazar;
        public ManualResetEvent mreAcquireAlazarReady;
        public ManualResetEvent mreAcquireAlazarRun;
        public ManualResetEvent mreAcquireAlazarKill;
        public ManualResetEvent mreAcquireAlazarDead;
        public AutoResetEvent areAcquireAlazarGo;
        public AutoResetEvent areAcquireAlazarComplete;
        public string strAcquireAlazarThreadStatus = "XAla";
        #endregion

        #region AcquireDAQThread
        public Thread threadAcquireDAQ;
        public ManualResetEvent mreAcquireDAQReady;
        public ManualResetEvent mreAcquireDAQRun;
        public ManualResetEvent mreAcquireDAQKill;
        public ManualResetEvent mreAcquireDAQDead;
        public AutoResetEvent areAcquireDAQGo;
        public AutoResetEvent areAcquireDAQComplete;
        public string strAcquireDAQThreadStatus = "XDAQ";
        #endregion

        #region AcquireIMAQThread
        public Thread threadAcquireIMAQ;
        public ManualResetEvent mreAcquireIMAQReady;
        public ManualResetEvent mreAcquireIMAQRun;
        public ManualResetEvent mreAcquireIMAQKill;
        public ManualResetEvent mreAcquireIMAQDead;
        public AutoResetEvent areAcquireIMAQGo;
        public AutoResetEvent areAcquireIMAQComplete;
        public string strAcquireIMAQThreadStatus = "XIMQ";
        #endregion

        #region SaveThread
        public Thread threadSave;
        public ManualResetEvent mreSaveReady;
        public ManualResetEvent mreSaveRun;
        public ManualResetEvent mreSaveKill;
        public ManualResetEvent mreSaveDead;
        public SemaphoreSlim ssSaveAction;
        public string strSaveThreadStatus = "XXX";
        public int nSaveNodeID; 
        #endregion

        #region ProcessThread
        public Thread threadProcess;
        public ManualResetEvent mreProcessReady;
        public ManualResetEvent mreProcessRun;
        public ManualResetEvent mreProcessKill;
        public ManualResetEvent mreProcessDead;
        public SemaphoreSlim ssProcessAction;
        public string strProcessThreadStatus = "XXX";
        #endregion

        // new process to process1 structures
        public ReaderWriterLockSlim rwls1;
        public int n1NumberCalibrationLines;
        public int n1NumberInterferenceLinesPerCalibration;
        public int n1LineLength;
        public float[] pf1Calibration;
        public float[] pf1Interference;
        public float[] pf1DAQ;

        // old structures
//        public ReaderWriterLockSlim rwlsProcessTo1;
        public int nProcess1WriteTimeout;
        public int nProcess1ProcessingType;
//        public UInt16[] pnProcess1Alazar;
//        public float[] pfProcess1DAQ;
//        public float[] pfProcess1IMAQParallel;
//        public float[] pfProcess1IMAQPerpendicular;
        public int nProcessNode = -1;
        public int nProcess1Node = -1;
//        public int nProcessTo1Data = 0;

        #region Process1Thread
        public Thread threadProcess1;
        public ManualResetEvent mreProcess1Ready;
        public ManualResetEvent mreProcess1Run;
        public ManualResetEvent mreProcess1Kill;
        public ManualResetEvent mreProcess1Dead;
        public ManualResetEvent mreProcess1Action;
        public string strProcess1ThreadStatus = "XXX";
        #endregion

        // new process1 to process2 structures;
        public ReaderWriterLockSlim rwls2;
        public int n2NumberSets;
        public int n2NumberDepthProfilesPerSet;
        public int n2LineLength;
        public float[] pf2Real;
        public float[] pf2Imag;
        public float[] pf2DAQ;
        public float[] pf2Calibration;
        public float[] pf2Interference;

        // old strucures
        public int nProcess2Type;

        public ReaderWriterLockSlim rwlsProcess1To2;
        public int nProcess2Node = -1;
        public int nProcess1To2Data = 0;

        public UInt16[] pnProcess2AAlazar;
        public float[] pfProcess2ADAQ;
        public float[] pfProcess2AIMAQParallel;
        public float[] pfProcess2AIMAQPerpendicular;

        public float[] pfProcess2ComplexRealParallel;
        public float[] pfProcess2ComplexImagParallel;
        public float[] pfProcess2ComplexRealPerpendicular;
        public float[] pfProcess2ComplexImagPerpendicular;

        public int nProcess2ANode = -1;
        public int nProcess1To2AData = 0;

        #region Process2Thread
        public Thread threadProcess2;
        public ManualResetEvent mreProcess2Ready;
        public ManualResetEvent mreProcess2Run;
        public ManualResetEvent mreProcess2Kill;
        public ManualResetEvent mreProcess2Dead;
        public ManualResetEvent mreProcess2Action;
        public string strProcess2ThreadStatus = "XXX";
        #endregion

#region CleanupThread
        public Thread threadCleanup;
        public ManualResetEvent mreCleanupReady;
        public ManualResetEvent mreCleanupRun;
        public ManualResetEvent mreCleanupKill;
        public ManualResetEvent mreCleanupDead;
        public ManualResetEvent mreCleanupAction;
        public string strCleanupThreadStatus = "XXX";
#endregion


        public void Initialize()
        {
#region MainThread
            mreMainReady = new ManualResetEvent(false);
            mreMainRun = new ManualResetEvent(false);
            mreMainKill = new ManualResetEvent(false);
            mreMainDead = new ManualResetEvent(false);
#endregion

#region OutputThread
            mreOutputReady = new ManualResetEvent(false);
            mreOutputRun = new ManualResetEvent(false);
            mreOutputKill = new ManualResetEvent(false);
            mreOutputDead = new ManualResetEvent(false);
            mreOutputUpdate = new ManualResetEvent(false);
#endregion

#region AcquireThread
            mreAcquireReady = new ManualResetEvent(false);
            mreAcquireRun = new ManualResetEvent(false);
            mreAcquireKill = new ManualResetEvent(false);
            mreAcquireDead = new ManualResetEvent(false);
            ssAcquireComplete = new SemaphoreSlim(0);
#endregion

            mreAcquireNodeReady = new ManualResetEvent(false);

#region AcquireAlazarThread
            mreAcquireAlazarReady = new ManualResetEvent(false);
            mreAcquireAlazarRun = new ManualResetEvent(false);
            mreAcquireAlazarKill = new ManualResetEvent(false);
            mreAcquireAlazarDead = new ManualResetEvent(false);
            areAcquireAlazarGo = new AutoResetEvent(false);
            areAcquireAlazarComplete = new AutoResetEvent(false);
#endregion

#region AcquireDAQThread
            mreAcquireDAQReady = new ManualResetEvent(false);
            mreAcquireDAQRun = new ManualResetEvent(false);
            mreAcquireDAQKill = new ManualResetEvent(false);
            mreAcquireDAQDead = new ManualResetEvent(false);
            areAcquireDAQGo = new AutoResetEvent(false);
            areAcquireDAQComplete = new AutoResetEvent(false);
#endregion

#region AcquireIMAQThread
            mreAcquireIMAQReady = new ManualResetEvent(false);
            mreAcquireIMAQRun = new ManualResetEvent(false);
            mreAcquireIMAQKill = new ManualResetEvent(false);
            mreAcquireIMAQDead = new ManualResetEvent(false);
            areAcquireIMAQGo = new AutoResetEvent(false);
            areAcquireIMAQComplete = new AutoResetEvent(false);
#endregion

#region SaveThead
            mreSaveReady = new ManualResetEvent(false);
            mreSaveRun = new ManualResetEvent(false);
            mreSaveKill = new ManualResetEvent(false);
            mreSaveDead = new ManualResetEvent(false);
            ssSaveAction = new SemaphoreSlim(0);
#endregion

#region ProcessThread
            mreProcessReady = new ManualResetEvent(false);
            mreProcessRun = new ManualResetEvent(false);
            mreProcessKill = new ManualResetEvent(false);
            mreProcessDead = new ManualResetEvent(false);
            ssProcessAction = new SemaphoreSlim(0);
#endregion

#region Process1Thread
            mreProcess1Ready = new ManualResetEvent(false);
            mreProcess1Run = new ManualResetEvent(false);
            mreProcess1Kill = new ManualResetEvent(false);
            mreProcess1Dead = new ManualResetEvent(false);
            mreProcess1Action = new ManualResetEvent(true);
#endregion

            rwlsProcess1To2 = new ReaderWriterLockSlim();

#region Process2Thread
            mreProcess2Ready = new ManualResetEvent(false);
            mreProcess2Run = new ManualResetEvent(false);
            mreProcess2Kill = new ManualResetEvent(false);
            mreProcess2Dead = new ManualResetEvent(false);
            mreProcess2Action = new ManualResetEvent(true);
#endregion

#region CleanupThead
            mreCleanupReady = new ManualResetEvent(false);
            mreCleanupRun = new ManualResetEvent(false);
            mreCleanupKill = new ManualResetEvent(false);
            mreCleanupDead = new ManualResetEvent(false);
            mreCleanupAction = new ManualResetEvent(false);
#endregion

        }   // public void Initialize

        public void Destroy()
        {
            ;
        }   // public void Destroy

    }   // public class CThreadData


    public class nOCTcudaWrapper : IDisposable
    {
        public const string gstrCUDAdll = "C:\\Users\\hylep\\Desktop\\nOCTcuda\\x64\\Release\\nOCTcuda.dll";

        bool disposed = false;
        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int getDeviceCount(ref int numberDevices);

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int getDeviceName(int deviceNumber, StringBuilder strDeviceName);

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int initialize(int nMode, int nRawNumberAlines, int nRawAlineLength, int nProcessNumberAlines, int nProcessedNumberAlines, int nProcessedAlineLength);

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int cleanup();

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int getDataAlazar();

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int calibrateMZI();

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(gstrCUDAdll)]
        public static extern int shutdown();

        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                disposed = true;
            }   // if (!disposed
        }   // public void Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //free managed ressources
            }   // if (disposing
            // free other ressources
        }   // protected virtual void Dispose

        ~nOCTcudaWrapper()
        {
            Dispose(false);
        }   // ~nOCTcudaWrapper

    }   // public class nOCTcudaWrapper


    public class nOCTippWrapper : IDisposable
    {
        bool disposed = false;

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Acquisition\\nOCTipp\\x64\\Release\\nOCTipp.dll")]
        public static extern int ippTest();

        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                disposed = true;
            }   // if (!disposed
        }   // public void Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //free managed ressources
            }   // if (disposing
            // free other ressources
        }   // protected virtual void Dispose

        ~nOCTippWrapper()
        {
            Dispose(false);
        }   // ~nOCTippWrapper

    }   // public class nOCTippWrapper


    public class nOCTimaqWrapper : IDisposable
    {
        bool disposed = false;



        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Hang\\Lab Razer\\nOCTImaq\\x64\\Debug\\nOCTImaq.dll")]
        public static extern int InitializeImaq(char[] interfaceName0, char[] interfaceName1, int nImaqLineLength, int nLinesPerChunk, int errInfo);

        
        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Hang\\Lab Razer\\nOCTImaq\\x64\\Debug\\nOCTImaq.dll")]
        public static extern void StartAcquisition();

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Hang\\Lab Razer\\nOCTImaq\\x64\\Debug\\nOCTImaq.dll")]
        public static extern void RealAcquisition0(int bufferIndex0, Int16[] pnTemp0);

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Hang\\Lab Razer\\nOCTImaq\\x64\\Debug\\nOCTImaq.dll")]
        public static extern void RealAcquisition1(int bufferIndex1, Int16[] pnTemp1);

        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("C:\\Users\\ONI-WORKSTATION-01\\Desktop\\Hang\\Lab Razer\\nOCTImaq\\x64\\Debug\\nOCTImaq.dll")]
        public static extern void StopAcquisition();

        
        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                disposed = true;
            }   // if (!disposed
        }   // public void Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //free managed ressources
            }   // if (disposing
            // free other ressources
        }   // protected virtual void Dispose

        ~nOCTimaqWrapper()
        {
            Dispose(false);
        }   // ~nOCTimaqWrapper

    }   // public class nOCTimaqWrapper

}
