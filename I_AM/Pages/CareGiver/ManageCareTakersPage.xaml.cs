using System.Collections.ObjectModel;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using I_AM.Services.Helpers;

namespace I_AM.Pages.CareGiver;

public partial class ManageCareTakersPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;
    public ObservableCollection<CaregiverInfo> CareTakers { get; set; }
    
    // Dictionary to track invitation IDs for accepted careTakers
    private Dictionary<string, string> _acceptedCareTakerInvitationIds = new();

    public ManageCareTakersPage()
    {
        InitializeComponent();
        CareTakers = new ObservableCollection<CaregiverInfo>();
        BindingContext = this;
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCareTakersAsync();
    }

    private void UpdateButtonVisibility()
    {
        // This method is called to update button visibility after loading
        // The visibility will be handled in the LoadCareTakersAsync by using a custom approach
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Force refresh the CollectionView to update button visibility
            if (CareTakersCollectionView != null)
            {
                CareTakersCollectionView.ItemsSource = null;
                CareTakersCollectionView.ItemsSource = CareTakers;
            }
        });
    }

    private async Task LoadCareTakersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: START");
            CareTakersLoadingIndicator.IsRunning = true;
            CareTakersLoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;
            CareTakers.Clear();

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: userId={userId}");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Missing userId or idToken");
                ShowError("Error: Unable to load user data");
                return;
            }

            // 1. Get pending invitations received by this caregiver (ToUserId == userId, Status == pending)
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetPendingInvitationsAsync");
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {pendingInvitations.Count} pending invitations");

            // 2. For accepted and rejected, we need to query all invitations where ToUserId == userId
            // Since GetAllCaregiverInvitationsAsync filters by fromUserId, we need to get all and filter manually
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Getting all invitations to find accepted/rejected");
            
            var allInvitations = new List<CaregiverInvitation>();
            var url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";
            
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDocument = System.Text.Json.JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;
                
                if (root.TryGetProperty("documents", out var documents))
                {
                    foreach (var doc in documents.EnumerateArray())
                    {
                        if (!doc.TryGetProperty("fields", out var fields))
                            continue;
                        
                        var toUserId = FirestoreValueExtractor.GetStringValue(fields, "toUserId");
                        var status = FirestoreValueExtractor.GetStringValue(fields, "status");
                        
                        // Only include invitations received by this caregiver
                        if (toUserId == userId && (status == "accepted" || status == "rejected"))
                        {
                            var invitation = new CaregiverInvitation
                            {
                                Id = FirestoreValueExtractor.GetDocumentId(doc),
                                FromUserId = FirestoreValueExtractor.GetStringValue(fields, "fromUserId"),
                                ToUserId = toUserId,
                                ToUserEmail = FirestoreValueExtractor.GetStringValue(fields, "toUserEmail"),
                                FromUserName = FirestoreValueExtractor.GetStringValue(fields, "fromUserName"),
                                Status = status,
                                CreatedAt = FirestoreValueExtractor.GetTimestampValue(fields, "createdAt"),
                                RespondedAt = FirestoreValueExtractor.GetTimestampValueNullable(fields, "respondedAt")
                            };
                            allInvitations.Add(invitation);
                        }
                    }
                }
            }
            
            var acceptedInvitations = allInvitations.Where(i => i.Status == "accepted").ToList();
            var rejectedInvitations = allInvitations.Where(i => i.Status == "rejected").ToList();

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: {pendingInvitations.Count} pending, {acceptedInvitations.Count} accepted, {rejectedInvitations.Count} rejected");

            // 3. Add pending invitations from careTakers
            foreach (var invitation in pendingInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding pending invitation from {invitation.FromUserName}");
                CareTakers.Add(new CaregiverInfo
                {
                    UserId = invitation.FromUserId,
                    Email = invitation.ToUserEmail,
                    FirstName = invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName,
                    LastName = invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty,
                    Status = "pending",
                    AddedAt = invitation.CreatedAt
                });
            }

            // 4. Add accepted invitations from careTakers
            foreach (var invitation in acceptedInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding accepted invitation from {invitation.FromUserName}");
                
                // For accepted invitations, fetch the careTaker's actual profile to get correct email
                var careTakerProfile = await _firestoreService.GetUserProfileAsync(invitation.FromUserId, idToken);
                
                var email = careTakerProfile?.Email ?? invitation.ToUserEmail;
                var firstName = careTakerProfile?.FirstName ?? (invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName);
                var lastName = careTakerProfile?.LastName ?? (invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty);
                
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] Accepted careTaker - UserId: {invitation.FromUserId}, Email: {email}, Name: {firstName} {lastName}");
                
                CareTakers.Add(new CaregiverInfo
                {
                    UserId = invitation.FromUserId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Status = "accepted",
                    AddedAt = invitation.CreatedAt
                });
            }

            // 5. Add rejected invitations from careTakers
            foreach (var invitation in rejectedInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding rejected invitation from {invitation.FromUserName}");
                
                // For rejected invitations, also fetch the careTaker's actual profile
                var careTakerProfile = await _firestoreService.GetUserProfileAsync(invitation.FromUserId, idToken);
                
                var email = careTakerProfile?.Email ?? invitation.ToUserEmail;
                var firstName = careTakerProfile?.FirstName ?? (invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName);
                var lastName = careTakerProfile?.LastName ?? (invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty);
                
                CareTakers.Add(new CaregiverInfo
                {
                    UserId = invitation.FromUserId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Status = "rejected",
                    AddedAt = invitation.CreatedAt
                });
            }

            if (CareTakers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: No careTakers found");
                NoCareTakersLabel.IsVisible = true;
            }
            else
            {
                NoCareTakersLabel.IsVisible = false;
            }
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: SUCCESS");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Exception - {ex.Message}\n{ex.StackTrace}");
            ShowError($"Error loading careTakers: {ex.Message}");
        }
        finally
        {
            CareTakersLoadingIndicator.IsRunning = false;
            CareTakersLoadingIndicator.IsVisible = false;
        }
    }

    private async void OnAddCareTakerClicked(object sender, EventArgs e)
    {
        if (!(sender is Button button)) return;

        var careTakerEmail = CareTakerEmailEntry.Text?.Trim() ?? string.Empty;

        // Validation
        if (string.IsNullOrWhiteSpace(careTakerEmail))
        {
            await DisplayAlert("Error", "CareTaker email is required", "OK");
            return;
        }

        if (!careTakerEmail.Contains("@"))
        {
            await DisplayAlert("Error", "Please provide a valid email", "OK");
            return;
        }

        button.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ErrorLabel.IsVisible = false;

        try
        {
            var currentUserId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();
            var currentUserEmail = await _authService.GetCurrentEmailAsync();

            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Current User ID: {currentUserId}");
            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Target email: {careTakerEmail}");

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(idToken))
            {
                ShowError("Error: Unable to load user data");
                return;
            }

            // Prevent adding yourself
            if (careTakerEmail.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "You cannot add yourself as a careTaker", "OK");
                return;
            }

            // 1. Get user profile by email
            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Calling GetUserPublicProfileByEmailAsync");
            var (careTakerPublicProfile, careTakerId) = await _firestoreService.GetUserPublicProfileByEmailAsync(careTakerEmail, idToken);

            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Result - Profile: {careTakerPublicProfile != null}, UserId: {careTakerId}");

            if (careTakerPublicProfile == null || string.IsNullOrEmpty(careTakerId))
            {
                await DisplayAlert("Error", "User with this email does not exist", "OK");
                return;
            }

            // 2. Create invitation
            var invitationId = Guid.NewGuid().ToString();
            var currentUserProfile = await _firestoreService.GetUserProfileAsync(currentUserId, idToken);

            if (currentUserProfile == null)
            {
                await DisplayAlert("Error", "Unable to load your profile", "OK");
                return;
            }

            var invitation = new CaregiverInvitation
            {
                Id = invitationId,
                FromUserId = currentUserId,
                ToUserId = careTakerId,
                ToUserEmail = careTakerEmail,
                FromUserName = $"{currentUserProfile.FirstName} {currentUserProfile.LastName}",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            // 3. Save invitation
            var saved = await _firestoreService.SaveCaregiverInvitationAsync(invitationId, invitation, idToken);

            if (saved)
            {
                await DisplayAlert("Success", $"Invitation sent to {careTakerEmail}", "OK");
                CareTakerEmailEntry.Text = string.Empty;
                // Reload careTakers list after sending invitation
                await LoadCareTakersAsync();
            }
            else
            {
                ShowError("Failed to send invitation");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnAcceptCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;
        
        // Only allow accepting pending invitations
        if (careTaker.Status != "pending")
        {
            await DisplayAlert("Informacja", "Mo¿na tylko zaakceptowaæ oczekuj¹ce zaproszenia", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Potwierdzenie",
            $"Czy chcesz zaakceptowaæ {careTaker.FirstName} {careTaker.LastName} jako podopiecznego?",
            "Tak",
            "Nie"
        );

        if (!result) return;

        try
        {
            button.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                return;
            }

            // Find the invitation ID for this careTaker
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
            var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);

            if (invitation == null)
            {
                await DisplayAlert("B³¹d", "Nie mo¿na znaleŸæ zaproszenia dla tego podopiecznego", "OK");
                return;
            }

            // Accept the invitation
            var success = await _firestoreService.AcceptCaregiverInvitationAsync(
                userId, 
                invitation.Id, 
                careTaker.UserId, 
                idToken);

            if (success)
            {
                // ===== NOWE: Inicjalizuj pytania =====
                var seedService = ServiceHelper.GetService<SeedQuestionsService>();
                var questionsInitialized = await seedService.InitializeDefaultQuestionsAsync(
                    careTaker.UserId,  // caretakerId
                    userId,            // caregiverId
                    idToken);

                if (questionsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[ManageCareTakersPage] Default questions initialized successfully");
                    await DisplayAlert(
                        "Sukces", 
                        $"Zaproszenie zaakceptowane od {careTaker.FirstName}\n\n Pytania zosta³y przygotowane",
                        "OK");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[ManageCareTakersPage] Failed to initialize questions");
                    await DisplayAlert(
                        "Uwaga", 
                        $"Zaproszenie zaakceptowane, ale pytania nie mog³y byæ przygotowane",
                        "OK");
                }
                // ===== KONIEC NOWEGO KODU =====

                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê zaakceptowaæ zaproszenia", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnRejectCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;
        
        // Only allow rejecting pending invitations
        if (careTaker.Status != "pending")
        {
            await DisplayAlert("Informacja", "Mo¿na tylko odrzuciæ oczekuj¹ce zaproszenia", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Potwierdzenie",
            $"Czy chcesz odrzuciæ zaproszenie od {careTaker.FirstName} {careTaker.LastName}?",
            "Tak",
            "Nie"
        );

        if (!result) return;

        try
        {
            button.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                return;
            }

            // Find the invitation ID for this careTaker
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
            var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);

            if (invitation == null)
            {
                await DisplayAlert("B³¹d", "Nie mo¿na znaleŸæ zaproszenia dla tego podopiecznego", "OK");
                return;
            }

            // Reject the invitation
            var success = await _firestoreService.RejectCaregiverInvitationAsync(userId, invitation.Id, idToken);

            if (success)
            {
                CareTakers.Remove(careTaker);
                
                if (CareTakers.Count == 0)
                {
                    NoCareTakersLabel.IsVisible = true;
                }

                await DisplayAlert("Sukces", "Zaproszenie odrzucone", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê odrzuciæ zaproszenia", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnRemoveCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;

        // Don't allow removing pending careTakers
        if (careTaker.Status == "pending")
        {
            await DisplayAlert("Informacja", "Nie mozna usunac oczekujacego zaproszenia. Prosze najpierw przyj ac lub odrzucic.", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Potwierdzenie",
            $"Czy na pewno chcesz usunac {careTaker.FirstName} {careTaker.LastName}?",
            "Tak",
            "Nie"
        );

        if (!result) return;

        try
        {
            button.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                await DisplayAlert("Blad", "Nie mozna zaladowac danych uzytkownika", "OK");
                return;
            }

            bool success = false;

            // For rejected status, find and delete rejected invitation
            if (careTaker.Status == "rejected")
            {
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Deleting {careTaker.Status} invitation for {careTaker.Email}");
                // Need to query all invitations to find the rejected one with ToUserId == userId
                var httpClient = new System.Net.Http.HttpClient();
                var url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
                
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDocument = System.Text.Json.JsonDocument.Parse(responseBody);
                    var root = jsonDocument.RootElement;
                    
                    if (root.TryGetProperty("documents", out var documents))
                    {
                        foreach (var doc in documents.EnumerateArray())
                        {
                            if (!doc.TryGetProperty("fields", out var fields))
                                continue;
                            
                            var toUserId = FirestoreValueExtractor.GetStringValue(fields, "toUserId");
                            var fromUserId = FirestoreValueExtractor.GetStringValue(fields, "fromUserId");
                            var status = FirestoreValueExtractor.GetStringValue(fields, "status");
                            var invitationId = FirestoreValueExtractor.GetDocumentId(doc);
                            
                            if (toUserId == userId && fromUserId == careTaker.UserId && status == "rejected")
                            {
                                success = await _firestoreService.DeleteCaregiverInvitationAsync(invitationId, idToken);
                                break;
                            }
                        }
                    }
                }
            }
            else if (careTaker.Status == "accepted")
            {
                // For accepted careTakers, remove from the relationship
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Removing accepted careTaker {careTaker.Email}");
                var httpClient = new System.Net.Http.HttpClient();
                var url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/caregiver_invitations?key={FirebaseConfig.WebApiKey}";
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
                
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDocument = System.Text.Json.JsonDocument.Parse(responseBody);
                    var root = jsonDocument.RootElement;
                    
                    if (root.TryGetProperty("documents", out var documents))
                    {
                        foreach (var doc in documents.EnumerateArray())
                        {
                            if (!doc.TryGetProperty("fields", out var fields))
                                continue;
                            
                            var toUserId = FirestoreValueExtractor.GetStringValue(fields, "toUserId");
                            var fromUserId = FirestoreValueExtractor.GetStringValue(fields, "fromUserId");
                            var status = FirestoreValueExtractor.GetStringValue(fields, "status");
                            var invitationId = FirestoreValueExtractor.GetDocumentId(doc);
                            
                            if (toUserId == userId && fromUserId == careTaker.UserId && status == "accepted")
                            {
                                success = await _firestoreService.DeleteCaregiverInvitationAsync(invitationId, idToken);
                                break;
                            }
                        }
                    }
                }
            }

            if (success)
            {
                CareTakers.Remove(careTaker);
                
                if (CareTakers.Count == 0)
                {
                    NoCareTakersLabel.IsVisible = true;
                }

                await DisplayAlert("Sukces", "Podopieczny zostal usuniety", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Blad", "Nie udalo sie usunac podopiecznego", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Blad", $"Blad: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
