   M S B A M L   `   `   `  ����   CountdownGo    CountdownGo.MainWindow   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -   $	��300��$	��400��  � WindowStartupLocation$  CenterScreen=����#1E1E1E���#CCCCCC � WindowStyle$
 None=� � AllowsTransparency$
 True�� � 	Resources    WindowButtonStyle �� 
TargetType �� Property �� Value �� Triggers P� Property	 P� Value��%E  &	        �� " ��.���  � ��
 ���� ��  � ��$ 0q���  � ��$ 46����  � ��$ 30����  � �� �#CCCCCC��  � ��$ Segoe MDL2 Assets)���  � ��$ 10(� P�  �@J�IsMouseOver$
	 True��. ���  � �� �#3F3F3FP�  � ��$
	 True��. ���  � �� �#007ACC.��� 
 � RowDefinitions
 �� $��30 ��� $��* ��� $
��Auto ��� $
��Auto �.+�� -   $��0�����#252526 � ColumnDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+��� $��1��$��CountdownGo�����#CCCCCC$��Center=�$
��10,0q�$��12(��� $��2�� �� Orientation$ 
Horizontal=�.+��� -   #��[  $	������ -   #��[  $	������ -   #��[  $	����
������ �� $��1��$��	25,5,25,5q����#3F3F3F$��1q� �� CornerRadius$ 4�� J� Effect 3  -System.Windows.Media.Effects.DropShadowEffect     ShadowDepth$ 2Q�   
BlurRadius$ 4Q�   Opacity$
 0.15Q�._�� ���#252526$
��10,5q�.+��� $ 
Horizontal=�$��Center=�$��Center=�.+�� -    �Name$ HourControl��$	��5,0q�.+��� -    ��Name$ HourDisplay��$��00��  TimeDisplayTextBlock#��[ �� $��:��#��[ $
��-5,0q�� -   $ MinuteControl��$	��5,0q�.+��� -	   $ MinuteDisplay��$��00��#��[ �� $��:��#��[ $
��-5,0q�� -
   $ SecondControl��$	��5,0q�.+��� -   $ SecondDisplay��$��00��#��[ �� $��2��$ 
Horizontal=�$��Center=�$��0,10,0,0q�.+��� -    ��Name$ Set10MinButton��$��10分钟��$��60��$��25��$��5q�  ModernButton#��[ �� -   $ Set20MinButton��$��20分钟��$��60��$��25��$��5q�#��[ �� -   $ Set30MinButton��$��30分钟��$��60��$��25��$��5q�#��[ �� -   $ Set45MinButton��$��45分钟��$��60��$��25��$��5q�#��[ �� $��3��$ 
Horizontal=�$��Center=�$��	0,10,0,20q�.+��� -   $ StartButton��$��开始��$��80��$��30��$��5q�#��[ �� -   $ PauseButton��$��暂停��$��80��$��30��$��5q�#��[ �� -   $ ResetButton��$��重置��$��80��$��30��$��5q�#��[ 