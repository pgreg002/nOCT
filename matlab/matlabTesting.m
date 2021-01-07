% close all;
clearvars; clc;

% parameters
strRawFilename = '2019_03_04_Mirror_8_110465.bin';
pnRawDataSize = [1024, 2048];
strRawDataType = 'double';

strReferenceFilename = '2019_03_04_Mirror_Ref_109405.bin';

bSingle = 0;

nCalibrationLeft = 344;
nCalibrationRight = 493;
nCalibrationRound = 16;

nModulationThreshold = 10;

nPaddingFactor = 16;



%% read data
fp = fopen(strRawFilename);
pdRaw = fread(fp, pnRawDataSize, strRawDataType);
fclose(fp);

fp = fopen(strReferenceFilename);
pdRef = fread(fp, pnRawDataSize, strRawDataType);
fclose(fp);
clear fp ans;

if (bSingle == 1)
    pdRaw = single(pdRaw);
    pdRef = single(pdRef);
end


%% initial pre-processing
% extract parameters
nRawAlineLength = size(pdRaw, 1);
nRawNumberAlines = size(pdRaw, 2);
nDepthProfileLength = floor(nRawAlineLength / 2 + 1);

% calculate reference
pdReference = mean(pdRef, 2);
pdRawMinusRef = pdRaw - repmat(pdReference, [1, size(pdRaw, 2)]);

% calibration peak mask
pdCalibrationMask = zeros([nRawAlineLength, 1]);
pdCalibrationMask((1:nCalibrationRound)+(nCalibrationLeft-nCalibrationRound-1), 1) = 0.5*(1+cos((nCalibrationRound:-1:1)*pi/nCalibrationRound));
pdCalibrationMask((1:nCalibrationRound)+(nCalibrationRight), 1) = 0.5*(1+cos((1:nCalibrationRound)*pi/nCalibrationRound));
pdCalibrationMask(nCalibrationLeft:nCalibrationRight) = 1;
pdZPCalibrationMask = zeros([nRawAlineLength*nPaddingFactor, 1]);
pdZPCalibrationMask((1:nCalibrationRound)+(nCalibrationLeft-nCalibrationRound-1), 1) = 0.5*(1+cos((nCalibrationRound:-1:1)*pi/nCalibrationRound));
pdZPCalibrationMask((1:nCalibrationRound)+(nCalibrationRight), 1) = 0.5*(1+cos((1:nCalibrationRound)*pi/nCalibrationRound));
pdZPCalibrationMask(nCalibrationLeft:nCalibrationRight) = 1;

% calibration arrays
pdX = (0 : nRawAlineLength-1)';
pdZPX = (0 : nRawAlineLength*nPaddingFactor-1)'/nPaddingFactor;

if (bSingle == 1)
    pdCalibrationMask = single(pdCalibrationMask);
    pdZPCalibrationMask = single(pdZPCalibrationMask);
    pdX = single(pdX);
    pdZPX = single(pdZPX);
end


fCubic = figure;
fZP = figure;


for nAline = 61; % : nRawNumberAlines

    % pull line for calibration
    pdCalibration = pdRawMinusRef(:, nAline);

    % fft to see initial depth profile
    pdDepthProfile = fft(pdCalibration);
    
    % mask out single peak
    pdCalibrationPeak = pdCalibrationMask .* pdDepthProfile;

    %% cubic spline method
    % generate phase ramp
    pdModulation = ifft(pdCalibrationPeak);
    pdAmplitude = abs(pdModulation);
    pnHighAmplitude = find(pdAmplitude > nModulationThreshold);
    if (bSingle == 1)
        pnHighAmplitude = single(pnHighAmplitude);
    end
    nLeft = pnHighAmplitude(1);
    nRight = pnHighAmplitude(end);
    pdPhaseRamp = unwrap(angle(ifft(pdCalibrationPeak)));

    % calculate fit
    dSlope = mean((pdPhaseRamp(nRight,:) - pdPhaseRamp(nLeft,:)) ./ (pdX(nRight,:)-pdX(nLeft,:)));
    dOffset = mean(pdPhaseRamp(nRight,:) - pdX(nRight,:) * dSlope);
    pdFit = dSlope * pdX + dOffset;

    % calculate corrected x-values
    pdXCorrected = pdX - (pdFit - pdPhaseRamp) / dSlope;
    
    % cubic spline interpolation
    pdCorrectedCalibration = spline(pdXCorrected, pdCalibration, pdX);
    pdCorrectedCalibration(nRight+1:end, :) = 0;
    pdCorrectedCalibration(1:nLeft-1, :) = 0;

    % calculate corrected peak
    pdCorrectedPeak = fft(pdCorrectedCalibration);

    % plot results
    figure(fCubic);
    subplot(2,2,1), plot(pdXCorrected, pdCalibration), title('calibration spectrum');
    hold on, plot(pdX, pdCorrectedCalibration, 'r'), hold off;

    subplot(2,2,2), plot(20*log10(abs(pdDepthProfile(1:nDepthProfileLength)))), title('calibration peak');
    hold on, plot(20*log10(abs(pdCalibrationPeak)), 'k', 'LineWidth', 2), hold off;
    hold on, plot(20*log10(abs(pdCorrectedPeak(1:nDepthProfileLength))), 'r'), hold off;
    ylim([0, 110]);

    subplot(2,2,3), plot(pdX, pdAmplitude), title('peak modulation amplitude');
    hold on, plot(pdX(nLeft:nRight), pdAmplitude(nLeft:nRight), 'k', 'LineWidth', 2), hold off;

    subplot(2,2,4), plot(pdX, pdPhaseRamp), title('peak phase ramp');
    hold on, plot(pdX(nLeft:nRight), pdPhaseRamp(nLeft:nRight), 'k', 'LineWidth', 2), hold off;
    hold on, plot(pdX, pdFit, 'r'), hold off;

    
    %% zero padding with linear interpolation
    % zero pad calibration spectrum
    pdPrepaddedDepthProfile = pdDepthProfile;
    pdPrepaddedDepthProfile(nDepthProfileLength, :) = 0.5 * pdPrepaddedDepthProfile(nDepthProfileLength, :);
    pdPaddedDepthProfile = zeros([nRawAlineLength*nPaddingFactor, 1]);
    if (bSingle == 1)
        pdPaddedDepthProfile = single(pdPaddedDepthProfile);
    end
    pdPaddedDepthProfile(1:nDepthProfileLength, :) = pdPrepaddedDepthProfile(1:nDepthProfileLength, :);
    pdPaddedDepthProfile(end-nDepthProfileLength+2:end, :) = pdPrepaddedDepthProfile(end-nDepthProfileLength+2:end, :);
    pdPaddedSpectrum = ifft(pdPaddedDepthProfile);

    % cut calibration peak
    pdZPCalibrationPeak = pdPaddedDepthProfile .* pdZPCalibrationMask;
    
    % generate phase ramp
    pdZPModulation = ifft(pdZPCalibrationPeak);
    pdZPAmplitude = abs(pdZPModulation);
    pnZPHighAmplitude = find(pdZPAmplitude > nModulationThreshold/nPaddingFactor);
    if (bSingle == 1)
        pnZPHighAmplitude = single(pnZPHighAmplitude);
    end
    nZPLeft = pnZPHighAmplitude(1);
    nZPRight = pnZPHighAmplitude(end);
    pdZPPhaseRamp = unwrap(angle(ifft(pdZPCalibrationPeak)));

    % calculate fit
    dZPSlope = mean((pdZPPhaseRamp(nZPRight,:) - pdZPPhaseRamp(nZPLeft,:)) ./ (pdZPX(nZPRight,:)-pdZPX(nZPLeft,:)));
    dZPOffset = mean(pdZPPhaseRamp(nZPRight,:) - pdZPX(nZPRight,:) * dZPSlope);
    pdZPFit = dZPSlope * pdZPX + dZPOffset;

    % calculate corrected x-values
    pdZPXCorrected = pdZPX - (pdZPFit - pdZPPhaseRamp) / dZPSlope;

    % linear interpolation
    pdZPCorrectedCalibration = interp1(pdZPXCorrected, pdPaddedSpectrum, pdX, 'linear', 'extrap');

    % calculate corrected peak
    pdZPCorrectedPeak = fft(pdZPCorrectedCalibration);

    figure;
    plot(pdZPX, nPaddingFactor*pdPaddedSpectrum);
    hold on, plot(pdX, pdCalibration, '+'), hold on;

    % plot results
    figure(fZP);
    subplot(2,2,1), plot(pdZPXCorrected, pdPaddedSpectrum), title('calibration spectrum');
    hold on, plot(pdX, pdZPCorrectedCalibration, 'r'), hold off;

    subplot(2,2,2), plot(20*log10(abs(pdPaddedDepthProfile(1:nDepthProfileLength)))), title('calibration peak');
    hold on, plot(20*log10(abs(pdZPCalibrationPeak)), 'k', 'LineWidth', 2), hold off;
    hold on, plot(20*log10(abs(nPaddingFactor*pdZPCorrectedPeak(1:nDepthProfileLength))), 'r'), hold off;
    ylim([0, 110]);

    subplot(2,2,3), plot(pdZPX, nPaddingFactor*pdZPAmplitude), title('peak modulation amplitude');
    hold on, plot(pdZPX(nZPLeft:nZPRight), nPaddingFactor*pdZPAmplitude(nZPLeft:nZPRight), 'k', 'LineWidth', 2), hold off;

    subplot(2,2,4), plot(pdZPX, pdZPPhaseRamp), title('peak phase ramp');
    hold on, plot(pdZPX(nZPLeft:nZPRight), pdZPPhaseRamp(nZPLeft:nZPRight), 'k', 'LineWidth', 2), hold off;
    hold on, plot(pdZPX, pdZPFit, 'r'), hold off;

    
end % for nAline
