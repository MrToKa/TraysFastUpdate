namespace TraysFastUpdate.Common.Constants;

public static class TrayConstants
{
    public const double SupportsWeight = 5.416;
    public const double KLDistance = 2;
    public const double WSLDistance = 5.5;
    public const double CProfileHeight = 15;
    public const double Spacing = 1;
    public const int TextPadding = 20;
    public const double GroundingCableWeight = 1.05;
    public const double GroundingCableDiameter = 95;

    public static class TrayTypes
    {
        public const string KL = "KL";
        public const string WSL = "WSL";
    }

    public static class TrayPurposes
    {
        public const string TypeA = "Type A (Pink color) for MV cables";
        public const string TypeB = "Type B (Green color) for LV cables";
        public const string TypeBC = "Type BC (Teal color) for LV and Instrumentation and  Control cables, divided by separator";
    }

    public static class CablePurposes
    {
        public const string Power = "Power";
        public const string Control = "Control";
        public const string MV = "MV";
        public const string VFD = "VFD";
    }

    public static class BundleTypes
    {
        // Cable diameter ranges that match CableService.DetermenCableDiameterGroup
        public const string Range0_8 = "0-8";
        public const string Range8_1_15 = "8.1-15";
        public const string Range15_1_21 = "15.1-21";
        public const string Range21_1_30 = "21.1-30";
        public const string Range30_1_40 = "30.1-40";
        public const string Range40_1_45 = "40.1-45";
        public const string Range45_1_60 = "45.1-60";
        public const string Range60Plus = "60+";

        // Legacy constants for backward compatibility
        public const string Power40_1_45 = "40.1-45";
        public const string Power45_60 = "45.1-60";
        public const string VFD30_1_40 = "30.1-40";
        public const string VFD40_1_45 = "40.1-45";
    }
}