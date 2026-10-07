[Variables]
; --- IP ADRESA ZARIADENIA ---
; Gateway address can be an IPv4 address or hostname.
; Examples: 192.168.2.95 | 10.20.30.40 | gw3000.home.lan
EcowittHost=
EcowittPort=80
EcowittWebUrl=https://www.ecowitt.net
DebugParser=0
MaxChannels=8

; --- NÁZVY KANÁLOV ---
; Ak je hodnota prázdna, použije sa názov z Ecowitt API.
; Pri názvoch s diakritikou je bezpečnejšie zadať alias sem.
Channel1Label=Kancelária M&M
Channel2Label=Predajňa
Channel3Label=Dielňa
Channel4Label=
Channel5Label=
Channel6Label=
Channel7Label=
Channel8Label=
GatewayLabel=Kancelária

; --- ŠTÝL TÉMY ILLUSTRO ---
fontName=Trebuchet MS
textSize=8
colorText=255,255,255,205
colorDim=255,255,255,140

; --- KOMFORTNÉ ROZSAHY INTERIÉRU ---
TempComfortMin=20
TempComfortMax=24
TempWarnMin=18
TempWarnMax=26
HumidityComfortMin=40
HumidityComfortMax=60
HumidityWarnMin=35
HumidityWarnMax=65

; --- FARBY STAVOV ---
ColorComfort=120,255,120,255
ColorWarning=255,210,80,255
ColorAlert=255,100,100,255
ColorCold=100,180,255,255

; --- OUTDOOR TEPLOTA ---
; WMO neurčuje univerzálne farebné prahy pre samotnú teplotu vzduchu.
; Toto sú praktické meteorologické pásma pre rýchlu orientáciu.
OutdoorTempExtremeColdMax=-20
OutdoorTempColdMax=-10
OutdoorTempFrostMax=0
OutdoorTempWarmMin=25
OutdoorTempHotMin=30
OutdoorTempVeryHotMin=35
ColorOutdoorExtremeCold=190,130,255,255
ColorOutdoorCold=100,180,255,255
ColorOutdoorFrost=100,220,255,255
ColorOutdoorNormal=255,255,255,205
ColorOutdoorWarm=255,210,80,255
ColorOutdoorHot=255,150,50,255
ColorOutdoorVeryHot=255,100,100,255

; --- UV INDEX (WHO/WMO medzinárodná škála) ---
; 0-2 nízky, 3-5 stredný, 6-7 vysoký, 8-10 veľmi vysoký, 11+ extrémny
ColorUVLow=40,149,0,255
ColorUVModerate=247,228,0,255
ColorUVHigh=248,89,0,255
ColorUVVeryHigh=216,0,29,255
ColorUVExtreme=107,73,200,255

; --- TRENDY A HISTÓRIA ---
; teplota sa porovnáva s hodnotou spred 15 minút
TrendTempMinutes=15
TrendTempThreshold=0.2
; tlak sa porovnáva s hodnotou spred 3 hodín
TrendPressureMinutes=180
TrendPressureThreshold=0.5
; história sa uloží každých 60 s a drží sa 4 hodiny
HistorySampleSeconds=60
HistoryKeepHours=4
; po koľkých sekundách bez rastu runtime považovať dáta za stale
StaleDataSeconds=60

; --- DYNAMICKÉ PREMENNÉ ---
V_TitleText=Meteo Stanica
V_TitleColor=255,255,255,205
V_Runtime=--

V_OutTemp=--
V_OutHumi=--
V_OutTempTrend=
V_OutTempColor=255,255,255,205
V_UV=--
V_UVColor=255,255,255,205
V_UVTextState=LOW
V_WindDir=--
V_RainLabel=
V_RainValue=
V_RuntimeHuman=--
V_BaroDelta3h=
V_OutTempTrendState=STABLE
V_WHTempTrendState=STABLE
V_R1TempTrendState=STABLE
V_R2TempTrendState=STABLE
V_R3TempTrendState=STABLE
V_BaroTrendState=STABLE
V_CH1Name=CH1
V_CH1Temp=--
V_CH1Humi=--
V_CH1Battery=--
V_CH2Name=CH2
V_CH2Temp=--
V_CH2Humi=--
V_CH2Battery=--
V_CH3Name=CH3
V_CH3Temp=--
V_CH3Humi=--
V_CH3Battery=--
V_CH4Name=CH4
V_CH4Temp=--
V_CH4Humi=--
V_CH4Battery=--
V_CH5Name=CH5
V_CH5Temp=--
V_CH5Humi=--
V_CH5Battery=--
V_CH6Name=CH6
V_CH6Temp=--
V_CH6Humi=--
V_CH6Battery=--
V_CH7Name=CH7
V_CH7Temp=--
V_CH7Humi=--
V_CH7Battery=--
V_CH8Name=CH8
V_CH8Temp=--
V_CH8Humi=--
V_CH8Battery=--
V_WindSpeed=--
V_WindGust=--
V_Solar=--
V_RainRate=--

V_WHTemp=--
V_WHHumi=--
V_WHTempTrend=
V_BaroAbs=--
V_BaroRel=--
V_BaroTrend=

V_R1Temp=--
V_R1Humi=--
V_R1TempTrend=
V_R2Temp=--
V_R2Humi=--
V_R2TempTrend=
V_R3Temp=--
V_R3Humi=--
V_R3TempTrend=

V_R1TempColor=255,255,255,205
V_R1HumiColor=255,255,255,205
V_R2TempColor=255,255,255,205
V_R2HumiColor=255,255,255,205
V_R3TempColor=255,255,255,205
V_R3HumiColor=255,255,255,205
V_WHTempColor=255,255,255,205
V_WHHumiColor=255,255,255,205

V_CH1DisplayName=CH1
V_CH1TrendState=STABLE
V_CH1TempColor=255,255,255,205
V_CH1HumiColor=255,255,255,205
V_CH1Hidden=1
V_CH1Y=128
V_CH2DisplayName=CH2
V_CH2TrendState=STABLE
V_CH2TempColor=255,255,255,205
V_CH2HumiColor=255,255,255,205
V_CH2Hidden=1
V_CH2Y=128
V_CH3DisplayName=CH3
V_CH3TrendState=STABLE
V_CH3TempColor=255,255,255,205
V_CH3HumiColor=255,255,255,205
V_CH3Hidden=1
V_CH3Y=128
V_CH4DisplayName=CH4
V_CH4TrendState=STABLE
V_CH4TempColor=255,255,255,205
V_CH4HumiColor=255,255,255,205
V_CH4Hidden=1
V_CH4Y=128
V_CH5DisplayName=CH5
V_CH5TrendState=STABLE
V_CH5TempColor=255,255,255,205
V_CH5HumiColor=255,255,255,205
V_CH5Hidden=1
V_CH5Y=128
V_CH6DisplayName=CH6
V_CH6TrendState=STABLE
V_CH6TempColor=255,255,255,205
V_CH6HumiColor=255,255,255,205
V_CH6Hidden=1
V_CH6Y=128
V_CH7DisplayName=CH7
V_CH7TrendState=STABLE
V_CH7TempColor=255,255,255,205
V_CH7HumiColor=255,255,255,205
V_CH7Hidden=1
V_CH7Y=128
V_CH8DisplayName=CH8
V_CH8TrendState=STABLE
V_CH8TempColor=255,255,255,205
V_CH8HumiColor=255,255,255,205
V_CH8Hidden=1
V_CH8Y=128
V_GatewayY=128
V_BaroSeparatorY=200
V_BaroAbsY=206
V_BaroRelY=222
V_PanelHeight=240

; --- PROJECT ---
ProjectWebsite=https://martinsturcel.sk
ProjectGitHub=https://github.com/mpca86/ecowitt-rainmeter
