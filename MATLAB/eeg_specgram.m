function [spec_mat,f,t] = eeg_specgram(signal, sample_freq,varargin)
    optional_args = length(varargin);    
    switch optional_args
        case 0
            [spec_mat,f,t] = specgram(signal, sample_freq);
        case 3
            win = varargin{1}; n_overlap = varargin{2}; nfft = varargin{3};
            [spec_mat,f,t] = specgram_window(signal, sample_freq,win,n_overlap, nfft);
    end
end

function [spec_mat,f,t] = specgram(signal, sample_freq)
    [C,f,t] = spectrogram(signal,[],[],[],sample_freq, 'yaxis');
    spec_mat = 10 * log10(abs(C));%C refers to colour at frequency f & time t
end

function [spec_mat,f,t] = specgram_window(signal, sample_freq, win_size, n_overlap, nfft)
    [C,f,t] = spectrogram(signal,win_size,n_overlap,nfft,sample_freq, 'yaxis');
    spec_mat = 10 * log10(abs(C));%C refers to colour at frequency f & time t
end