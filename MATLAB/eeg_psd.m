function [pwr_y,freq_x] = eeg_psd(signal, sample_freq)
    [pwr_y, freq_x] = pwelch(signal,[],[],length(signal),sample_freq);
end