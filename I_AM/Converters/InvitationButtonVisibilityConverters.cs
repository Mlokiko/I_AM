using System.Globalization;
using I_AM.Models;

namespace I_AM.Converters;

/// <summary>
/// Converter to determine if Accept/Reject buttons should be visible
/// Shows only for received pending invitations (IsSentByMe = false AND Status = "pending")
/// </summary>
public class ShowAcceptRejectButtonsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CaregiverInfo caregiverInfo)
        {
            return !caregiverInfo.IsSentByMe && caregiverInfo.Status == "pending";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to determine if Delete button should be visible
/// Shows for:
/// - Sent invitations (IsSentByMe = true AND (Status = "pending" OR Status = "rejected"))
/// - Accepted invitations (Status = "accepted")
/// </summary>
public class ShowDeleteButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CaregiverInfo caregiverInfo)
        {
            // Show delete for accepted status regardless of who sent it
            if (caregiverInfo.Status == "accepted")
                return true;

            // Show delete for sent pending or rejected invitations
            if (caregiverInfo.IsSentByMe && (caregiverInfo.Status == "pending" || caregiverInfo.Status == "rejected"))
                return true;

            return false;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
