function eeg_pwr = eeg_bandpower(x,fs,freqRange)
    eeg_pwr = bandpower_r2014a(x,fs,freqRange);
end