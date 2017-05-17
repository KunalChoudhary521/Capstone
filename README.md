# Capstone Project -- Sleep Apnea Diagnoser

This software package was designed as a capstone project in ECE496. The goal of this project was
to help the Sleep Science team at the Toronto Rehabilitation Institution to analyzes physiological 
signals in order determine severity of Sleep Apnea. Sleep Apnea is a disorder that causes periods 
of shallow or absent breathing while sleeping.

Originally, the project was hosted on Team Foundation Server (TFS). I have recently migrated the 
project from TFS to Github. As a result, most of my commit messages are recorded under the label
'Kunal-CUSTOM' rather than KunalChoudhary521.

This software package analyzes 2 types of physiological signals: Electroencephalogram (EEG) and
Respiratory signals. I was mainly responsible for analyzing EEG signals and displaying the results 
as plots on a Graphical User Interface (GUI). EEG analysis is mostly in [Class_Analysis_EEG.cs](https://github.com/KunalChoudhary521/Capstone/blob/master/SleepApneaAnalysisTool/SleepApneaAnalysisTool/Class_Analysis_EEG.cs)
within functions: 
[BW_EEGCalculations(...)](https://github.com/KunalChoudhary521/Capstone/blob/cb4208596bd62ced775800dcadbfaa15f1185f1a/SleepApneaAnalysisTool/SleepApneaAnalysisTool/Class_Analysis_EEG.cs#L563) and [BW_EEGAnalysisEDF(...)](https://github.com/KunalChoudhary521/Capstone/blob/cb4208596bd62ced775800dcadbfaa15f1185f1a/SleepApneaAnalysisTool/SleepApneaAnalysisTool/Class_Analysis_EEG.cs#L703). 

The GUI code is in [MainWindow.xaml](https://github.com/KunalChoudhary521/Capstone/blob/cb4208596bd62ced775800dcadbfaa15f1185f1a/SleepApneaAnalysisTool/SleepApneaAnalysisTool/MainWindow.xaml#L295).

The following shows a snapshot of resultant plots after performing EEG Analysis on a signal named: (F4 - M1)
![GUI EEG Analysis](https://github.com/KunalChoudhary521/Capstone/blob/master/2%20All-Plots.png "EEG Analysis")


Project Contributors:
* Kunal Choudhary
* Zabeeh Ur-Rahman
* Mohamed Maria


Project Supervisors:
* Dr. Azadeh Yahdollahi
* Yingxuan (Derek) Zhi
