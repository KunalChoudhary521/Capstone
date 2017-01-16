function [spec_mat, time] = eeg_specgram(signal, sample_freq)
    [res,~,time] = spectrogram(signal,[],[],[],sample_freq);
    spec_mat = abs(res);%calculate magnitude of complex number matrix
end