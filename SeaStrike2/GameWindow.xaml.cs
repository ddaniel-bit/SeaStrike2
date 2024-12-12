using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeaStrike2
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private string partyid;
        private int clientid;
        private int[,] _gridMatrix;
        private readonly string _firebaseDatabaseUrl = "https://seastrike2szs-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly string _firebaseApiKey = "AIzaSyBbftGCHaor_waWMIKFWrE_JnVw2gNPEzI";
        private Dictionary<string, string> clientsData;
        private bool elsoletetel = true;
        private CancellationTokenSource _cancellationTokenSource;
        public GameWindow(string partyID, int clientID, int[,] gridMatrix)
        {
            InitializeComponent();
            partyid = partyID;
            clientid = clientID;
            _gridMatrix = gridMatrix;
            ReplaceNegativeOneWithZero();
            UpdateClientsData();
            MatrixBejelolese();
            if (clientid == 1 && elsoletetel)
            {
                TeJossz();
            }
            else {
                NemTeJossz();
            }

        }
        private void ReplaceNegativeOneWithZero()
        {
            for (int i = 0; i < _gridMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < _gridMatrix.GetLength(1); j++)
                {
                    if (_gridMatrix[i, j] == -1)
                    {
                        _gridMatrix[i, j] = 0;
                    }
                }
            }
        }
        private void MatrixBejelolese()
        {
            for (int i = 0; i < _gridMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < _gridMatrix.GetLength(1); j++)
                {
                    if (_gridMatrix[i, j] == 1)
                    {
                        
                        int x = i+1;
                        int y = j+1;

                        var button = FindGridButtonByTag(x, y);
                        if (button != null)
                        {
                            // A gomb háttérszínének módosítása pirosra
                            button.Background = new SolidColorBrush(Color.FromRgb(190, 190, 190));
                        }
                    }
                }
            }
        }
        private async void GridButton_Click(object sender, RoutedEventArgs e)
        {
            // A gombot az 'sender' objektum formájában kapjuk
            Button clickedButton = (Button)sender;

            // Az értéket a 'Tag' tulajdonságból olvassuk ki
            string value = (string)clickedButton.Tag;
            UpdateClientsData();
            if (clientid == 1 && elsoletetel)
            {
                NemTeJossz();
                elsoletetel = false;
                TorpedoFirebase(partyid, value);
                clickedButton.Background = Brushes.Gray;
                clickedButton.Content = "🧨";
            }
            else if (int.TryParse(clientsData["WhoIsNext"], out int whoIsNext) && whoIsNext == clientid)
            {
                NemTeJossz();
                elsoletetel = false;
                TorpedoFirebase(partyid, value);
                clickedButton.Background = Brushes.Gray;
                clickedButton.Content = "🧨";
            }
            else {
                MessageBox.Show("nem te vagy soron "+ clientsData["WhoIsNext"]);
            }
        }
        private async Task TorpedoFigyelo(string tableName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Ellenőrizzük a Client2Message mezőt
                string client2Message = await GetClient2MessageAsync(tableName);
                string whoisnext = await GetWhoIsNext(partyid);

                // Ha a Client2Message módosult
                if (client2Message != null && client2Message != "")
                {
                    // Ha a Client2Message tartalmazza a 'hit' szót
                    if (client2Message.Contains("hit") && int.Parse(whoisnext) == clientid)
                    {
                        // Kivonjuk a koordinátákat a 'hit(x,y)' szövegből
                        string coordinates = client2Message.Substring(4, client2Message.Length - 5); // Az 'hit(' és ')' között
                        string[] coords = coordinates.Split(',');

                        int x = int.Parse(coords[0]);
                        int y = int.Parse(coords[1]);

                        // A megfelelő gomb keresése a koordináták alapján
                        var button = FindGridButtonByTag(x, y);
                        if (button != null)
                        {
                            // A gomb háttérszínének módosítása pirosra
                            button.Background = Brushes.LightSeaGreen;
                            button.Content = "🧨";
                            TalalatVisszajelzesFirebase(partyid, coordinates);
                        }

                        TeJossz();
                        _cancellationTokenSource.Cancel();
                    }
                }

                await Task.Delay(1000); // Várunk egy másodpercet a következő ellenőrzés előtt
            }
        }
        private Button FindGridButtonByTag(int x, int y)
        {
            // Minden gomb végigjárása a gridben
            foreach (var child in GameGrid.Children)
            {
                if (child is Button button)
                {
                    // A gomb Tag tulajdonságának ellenőrzése
                    if (button.Tag is string tag && tag == $"{x},{y}")
                    {
                        return button;
                    }
                }
            }
            return null; // Ha nem találunk megfelelőt, null-t adunk vissza
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
                if (clientid == 1)
                {
                    return tableData?.Client2Message;
                }
                else
                {
                    return tableData?.Client1Message;
                }

            }
        }
        private async Task<string> GetWhoIsNext(string tableName)
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
                if (clientid == 1)
                {
                    return tableData?.WhoIsNext;
                }
                else
                {
                    return tableData?.WhoIsNext;
                }

            }
        }
        private void TeJossz()
        {
            lblYou.Visibility = Visibility.Visible;
            lblOpp.Visibility = Visibility.Collapsed;
            Talalte();
        }
        private async void NemTeJossz()
        {
            lblYou.Visibility = Visibility.Collapsed;
            lblOpp.Visibility = Visibility.Visible;
            _cancellationTokenSource = new CancellationTokenSource();
            await TorpedoFigyelo(partyid, _cancellationTokenSource.Token);
            UpdateClientsData();
        }

        private async void Talalte()
        {
            clientsData = await GetClientsData(partyid);

            // Kiírás MessageBox-ba az összes adatot
            //string message = string.Join(Environment.NewLine, clientsData.Select(kvp => $"Key: {kvp.Key}, Value: {kvp.Value}"));
            //MessageBox.Show(message, "Clients Data");

            // lbUsername.Content beállítása a clientid alapján
            if (clientid == 1 && clientsData["Client2TalalatErt"].Contains(","))
            {
                string[] coords = clientsData["Client2TalalatErt"].Split(',');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                // A megfelelő gomb keresése a koordináták alapján
                var button = FindGridButtonByTag(x, y);
                if (button != null)
                {
                    // A gomb háttérszínének módosítása pirosra
                    button.Background = Brushes.Green;
                    button.Content = "💥";
                }
            }
            else if (clientid == 2 && clientsData["Client1TalalatErt"].Contains(","))
            {
                string[] coords = clientsData["Client1TalalatErt"].Split(',');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                // A megfelelő gomb keresése a koordináták alapján
                var button = FindGridButtonByTag(x, y);
                if (button != null)
                {
                    // A gomb háttérszínének módosítása pirosra
                    button.Background = Brushes.Green;
                }
            }
        }

        private async Task TorpedoFirebase(string tableName, string coord)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";

                // Inicializáljuk a 'data' változót
                object data;

                if (clientid == 1)
                {
                    // Client1 adatainak frissítése
                    data = new
                    {
                        Client1Message = $"hit({coord})",
                        WhoIsNext = "2"
                    };
                    
                }
                else if (clientid == 2)
                {
                    // Client2 adatainak frissítése
                    data = new
                    {
                        Client2Message = $"hit({coord})",
                        WhoIsNext = "1"
                    };
                    
                }
                else
                {
                    throw new Exception("Érvénytelen clientid érték!");
                }

                // JSON-ba sorosítjuk az adatokat
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // PATCH kérést küldünk
                var response = await client.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Nem sikerült módosítani a táblát.");
                }

            }
        }
        private string MatrixToString(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            string result = "";

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result += matrix[i, j] + " ";
                }
                result += "\n";
            }

            return result;
        }
        private async Task TalalatVisszajelzesFirebase(string tableName, string kapottcoord)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_firebaseDatabaseUrl}{tableName}.json?auth={_firebaseApiKey}";

                // Inicializáljuk a 'data' változót
                object data;
                string[] xy = kapottcoord.Split(',');
                string formattedMatrix = MatrixToString(_gridMatrix);

                // Show the formatted matrix in a MessageBox
                //MessageBox.Show(formattedMatrix, "Matrix State", MessageBoxButton.OK, MessageBoxImage.Information);
                //MessageBox.Show($"DEBUG: koordináta: {kapottcoord} konvertált: {int.Parse(xy[0]) - 1},{int.Parse(xy[1]) - 1}, érték a mátrixban: {_gridMatrix[int.Parse(xy[0]) - 1, int.Parse(xy[1]) - 1]}");
                if (_gridMatrix[int.Parse(xy[0])-1, int.Parse(xy[1])-1] == 1)
                {
                    if (clientid == 1)
                    {
                        // Client1 adatainak frissítése
                        data = new
                        {
                            Client1TalalatErt = $"{kapottcoord}"
                        };

                    }
                    else if (clientid == 2)
                    {
                        // Client2 adatainak frissítése
                        data = new
                        {
                            Client2TalalatErt = $"{kapottcoord}"
                        };

                    }
                    else
                    {
                        throw new Exception("Érvénytelen clientid érték!");
                    }
                    
                    int x = int.Parse(xy[0]);
                    int y = int.Parse(xy[1]);

                    var button = FindGridButtonByTag(x, y);
                    if (button != null)
                    {
                        // A gomb háttérszínének módosítása pirosra
                        button.Background = Brushes.Red;
                        button.Content = "💀";
                    }
                }
                else
                {
                    if (clientid == 1)
                    {
                        // Client1 adatainak frissítése
                        data = new
                        {
                            Client1TalalatErt = ""
                        };

                    }
                    else if (clientid == 2)
                    {
                        // Client2 adatainak frissítése
                        data = new
                        {
                            Client2TalalatErt = ""
                        };

                    }
                    else
                    {
                        throw new Exception("Érvénytelen clientid érték!");
                    }
                }
                // JSON-ba sorosítjuk az adatokat
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // PATCH kérést küldünk
                var response = await client.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Nem sikerült módosítani a táblát.");
                }

            }
        }

        private async void UpdateClientsData()
        {
            clientsData = await GetClientsData(partyid);

            // Kiírás MessageBox-ba az összes adatot
            //string message = string.Join(Environment.NewLine, clientsData.Select(kvp => $"Key: {kvp.Key}, Value: {kvp.Value}"));
            //MessageBox.Show(message, "Clients Data");

            // lbUsername.Content beállítása a clientid alapján
            if (clientid == 1 && clientsData.ContainsKey("Client2Username"))
            {
                lbUsername.Content = $"•{clientsData["Client2Username"]}";
            }
            else if (clientid == 2 && clientsData.ContainsKey("Client1Username"))
            {
                lbUsername.Content = $"•{clientsData["Client1Username"]}";
            }
            else
            {
                lbUsername.Content = "N/A"; // Ha nincs megfelelő kulcs
            }
        }


        private async Task<Dictionary<string, string>> GetClientsData(string tableName)
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
                    return new Dictionary<string, string>(); // Ha nincs adat, üres szótárat adunk vissza
                }

                var tableData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                Dictionary<string, string> otherUserData = new Dictionary<string, string>();

                // Explicit konverzió stringre a dynamic objektumból
                if (tableData != null)
                {
                    otherUserData.Add("Client1Matrix", tableData.Client1Matrix != null ? tableData.Client1Matrix.ToString() : "N/A");
                    otherUserData.Add("Client1Username", tableData.Client1Username != null ? tableData.Client1Username.ToString() : "N/A");
                    otherUserData.Add("Client1TalalatErt", tableData.Client1TalalatErt != null ? tableData.Client1TalalatErt.ToString() : "N/A");
                    otherUserData.Add("Client2Matrix", tableData.Client2Matrix != null ? tableData.Client2Matrix.ToString() : "N/A");
                    otherUserData.Add("Client2Username", tableData.Client2Username != null ? tableData.Client2Username.ToString() : "N/A");
                    otherUserData.Add("Client2TalalatErt", tableData.Client2TalalatErt != null ? tableData.Client2TalalatErt.ToString() : "N/A");
                    otherUserData.Add("WhoIsNext", tableData.WhoIsNext != null ? tableData.WhoIsNext.ToString() : "N/A");
                }
                if (true)
                {

                }
                return otherUserData;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
