using CommunityToolkit.Mvvm.ComponentModel;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MLauncher.Sections;

public partial class MyPlanView : UserControl
{
    public MyPlanView()
    {
        InitializeComponent();
    }

    private void ChangePayment_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.paypal.com/myaccount/autopay/",
            UseShellExecute = true
        });
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight - 150)
        {
            var _ = Launch.instance.myPlan.FetchTransactionsData();
        }
    }

    private void Cta_Click(object sender, RoutedEventArgs e)
    {
        Button? btn = sender as Button;
        string? btnText = btn?.Content.ToString();
        if (btnText == "Update plan")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{Constants.WebURL}/pricing",
                UseShellExecute = true
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{Constants.WebURL}/my-plan",
                UseShellExecute = true
            });
        }
    }
}

public partial class MyPlan : ObservableObject
{
    [ObservableProperty] private bool hasPaymentInfo = false;
    [ObservableProperty] private string cta = "Update plan";
    [ObservableProperty] private string planName = "MANUAL FREE";

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private ProductDetails? subscription;

    public async Task FetchSubscriptionData()
    {
        PlanName = $"MANUAL {User.Current?.Products["manual"]?.Plan?.ToUpper()}";
        IsLoading = true;

        try
        {
            string token = FileManager.LoadToken(Login.tpath);
            var subscriptionData = await WebManager.GET<ProductDetails>($"{Constants.WebURL}/api/user/subscription", token);

            if (subscriptionData.Pay == false)
            {
                subscriptionData.NextBillingTime = "Plan granted by the Manual team.";
            }
            else
            {
                if (DateTime.TryParse(subscriptionData.NextBillingTime, null, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                {
                    string formattedDate = parsedDate.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
                    subscriptionData.NextBillingTime = $"Your plan will resume on {formattedDate}.";
                }
                HasPaymentInfo = true;
                Cta = "Cancel plan";
            }

            Subscription = subscriptionData;
        }
        catch (Exception)
        {
            MessageBox.Show("The subscription information could not be uploaded. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ObservableProperty] private bool isTransactionsLoading;
    public ObservableCollection<TransactionDetails> Transactions { get; set; } = new();
    private int currentYearOffset = 0;
    private bool hasMoreTransactions = true;

    public async Task FetchTransactionsData()
    {
        if (!hasMoreTransactions || IsTransactionsLoading) return;
        IsTransactionsLoading = true;

        var endDate = DateTime.UtcNow.AddYears(-currentYearOffset).ToString("o");
        var startDate = DateTime.UtcNow.AddYears(-currentYearOffset - 1).ToString("o");

        try
        {
            string token = FileManager.LoadToken(Login.tpath);
            var transactions = await WebManager.GET<ObservableCollection<TransactionDetails>>($"{Constants.WebURL}/api/user/transactions?startDate={startDate}&endDate={endDate}", token);

            if (transactions == null || transactions.Count == 0)
            {
                hasMoreTransactions = false;
                return;
            }

            foreach (var transaction in transactions)
            {
                if (DateTime.TryParse(transaction.Time, null, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                {
                    transaction.Time = parsedDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                }
                Transactions.Add(transaction);
            }

            currentYearOffset++;
        }
        catch (Exception)
        {
            MessageBox.Show("The transactions information could not be uploaded. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            hasMoreTransactions = false;
        }
        finally
        {
            IsTransactionsLoading = false;
        }
    }
}
