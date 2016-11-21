function out_array = m_resample(in_array, factor)
[N,D] = rat(factor);
out_array = resample(in_array, N, D);