using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace SeaStrike2
{
    public partial class MainWindow : Window
    {
        private readonly string _firebaseDatabaseUrl = "https://seastrike2szs-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly string _firebaseApiKey = "AIzaSyBbftGCHaor_waWMIKFWrE_JnVw2gNPEzI"; // Firebase projekt API kulcs

        public MainWindow()
        {
            InitializeComponent();
        }

        private CancellationTokenSource _cancellationTokenSource;

        private async void btnPartyCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tableName = GenerateRandomString(5);
                await CreateTableInFirebaseAsync(tableName);
                MessageBox.Show($"Party létrehozva: {tableName}", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                // Indítjuk a folyamatos ellenőrző ciklust
                _cancellationTokenSource = new CancellationTokenSource();
                await MonitorClient2MessageAsync(tableName, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private async Task MonitorClient2MessageAsync(string tableName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Ellenőrizzük a Client2Message mezőt
                string client2Message = await GetClient2MessageAsync(tableName);

                // Ha a Client2Message módosult
                if (client2Message != null && client2Message != "")
                {
                    
                    if (client2Message == "joined")
                    {
                        LayoutEditortemp layoutedit = new LayoutEditortemp(tableName, 1);
                        layoutedit.Show();
                        this.Close();
                        break;
                    }
                    
                }

                await Task.Delay(1000); // Várunk egy másodpercet a következő ellenőrzés előtt
            }
        }

        private async Task<string> GetClient2MessageAsync(string tableName)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Nem sikerült lekérdezni a táblát. HTTP státusz: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                if (jsonResponse == "null")
                {
                    return null; // Ha nincs adat, visszatérünk null-lal
                }

                var tableData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                return tableData?.Client2Message;
            }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        private async Task CreateTableInFirebaseAsync(string tableName)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";

                // Define the data to be added to the new table
                var data = new
                {
                    Client1Message = "wait",
                    Client1Username = "Client1",
                    Client1Matrix = "",
                    Client1TalalatErt = "",
                    Client2Message = "",
                    Client2Username = "",
                    Client2Matrix = "",
                    Client2TalalatErt = "",
                    WhoIsNext = ""
                };

                // Serialize the data to JSON
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Send the data to Firebase
                var response = await client.PutAsync(url, content);

                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Nem sikerült a tábla létrehozása. HTTP státusz: {response.StatusCode}");
                }
            }
        }


        private async void btnPartyJoin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tableName = txtPartyCode.Text.Trim(); // A party kód, amit a felhasználó adott meg

                // Ellenőrizzük, hogy a tábla létezik-e
                if (await TableExistsAsync(tableName))
                {
                    // Ha létezik a tábla, módosítjuk
                    await ModifyTableAsync(tableName);
                    LayoutEditortemp layoutedit = new LayoutEditortemp(tableName, 2);
                    layoutedit.Show();
                    this.Close();
                }
                else
                {
                    // Ha nem létezik a tábla
                    MessageBox.Show("Nincs ilyen party!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";

                try
                {
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        return false; // Ha a válasz nem sikeres, akkor nem létezik a tábla
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return jsonResponse != "null"; // Ha a válasz "null", akkor nem létezik a tábla
                }
                catch (Exception)
                {
                    return false; // Hibás kérés esetén is visszatérünk false értékkel
                }
            }
        }

        private async Task ModifyTableAsync(string tableName)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";

                // Az adatokat, amiket módosítani szeretnénk
                var data = new
                {
                    Client2Message = "joined",
                    Client2Username = "Client2",
                    Client2Matrix = ""
                };

                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // A PATCH kérést küldjük
                var response = await client.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Nem sikerült módosítani a táblát.");
                }

                // Ellenőrizzük, hogy valóban frissült-e
                var updatedData = await GetClient2MessageAsync(tableName);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            // Az alkalmazás bezárása, leállítjuk a ciklust is
            _cancellationTokenSource?.Cancel();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove(); // Az ablak húzása egérrel
            }
        }

        private void btnCredits_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
