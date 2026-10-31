namespace RepairShop.Domain.Modules.Inspections;

public enum InspectionType
{
    Functional,
    Appearance,
    RepairVerification
}

public enum FunctionalInspectionStatus
{
    Pass,
    Fail,
    NotApplicable,
    NotTested
}

public enum AppearanceInspectionStatus
{
    Normal,
    Damaged,
    Unknown
}

public enum RepairVerificationStatus
{
    Fixed,
    PartiallyFixed,
    NotFixed,
    UnableToVerify
}
