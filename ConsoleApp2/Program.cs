using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using SteamKit2;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.GC;

namespace Sample5_SteamGuard
{
    class Program
    {
        static SteamClient steamClient;
        static CallbackManager manager;

        static SteamUser steamUser;

        static bool isRunning;

        static string user, pass;
        static string authCode, twoFactorAuth;
        static SteamFriends steamFriends;
        static SteamID steamIdFriend;

        static SteamGameCoordinator steamGC;

        static void Main(string[] args)
        {

            Console.OutputEncoding = Encoding.UTF8;
            user = "monkeykingkill4r";
            pass = "Cqt44cqt";

            // создаем ваш экземпляр клиента steam
            steamClient = new SteamClient();
            // создаем менеджер обратного вызова, который будет направлять обратные вызовы на вызовы функций
            manager = new CallbackManager(steamClient);

            // получаем обработчик steamuser, который используется для входа в систему после успешного подключения
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            //подключаемся к gc
            steamGC = steamClient.GetHandler<SteamGameCoordinator>();
            // зарегистрируем несколько обратных вызовов, в которых мы заинтересованы
            // они регистрируются при создании в менеджере обратных вызовов, который затем направляет обратные вызовы
            // к указанным функциям
            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

          
            // этот обратный вызов запускается, когда серверы steam хотят, чтобы клиент сохранил файл sentry

            manager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);
            manager.Subscribe<SteamFriends.ProfileInfoCallback>(OnProfileInfo);

            

            manager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
            isRunning = true;

            Console.WriteLine("Подключение к Steam...");

            // инициируем соединение
            steamClient.Connect();

            // создаем ваш цикл обработки обратных вызовов
            while (isRunning)
            {
                // для того, чтобы обратные вызовы были направлены, они должны быть обработаны менеджером
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Подключено! Вход произведён под логином  '{0}'...", user);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin"))
            {
                // если у нас есть сохраненный файл часового, прочитайте и sha-1 хэширует его
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");

            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = user,
                Password = pass,

                // в этом примере мы передаем дополнительный код авторизации
                // это значение будет нулевым (по умолчанию) для нашей первой попытки входа
                AuthCode = authCode,

                // если в аккаунте используется двухфакторная аутентификация, вместо этого мы предоставим двухфакторный код
                // это также будет нулевым при нашей первой попытке входа
                TwoFactorCode = twoFactorAuth,

                // наши последующие входы в систему используют хэш файла часового в качестве доказательства владения файлом
                // это также будет нулевым для наших первых (без кода авторизации) и второй (только для кода авторизации) попыток входа
            });
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            // после получения входа в аккаунт отказано, мы будем отключены от Steam
            // поэтому после того, как мы прочитали код авторизации от пользователя, нам нужно повторно подключиться, чтобы снова начать поток входа

            Console.WriteLine("Отключено Steam, переподключение через 5...");

            Thread.Sleep(TimeSpan.FromSeconds(5));

            steamClient.Connect();
        }


        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            if (isSteamGuard || is2FA)
            {
                Console.WriteLine("Аккаунт защищён SteamGuard!");

                if (is2FA)
                {
                    Console.Write("Ввведите код аутентификации с телефона: ");
                    twoFactorAuth = Console.ReadLine();
                }
                else
                {
                    Console.Write("Код аутентификации с почты {0}: ", callback.EmailDomain);
                    authCode = Console.ReadLine();
                }

                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Неверный логин: {0} / {1}", callback.Result, callback.ExtendedResult);

                isRunning = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");

            // на этом этапе мы сможем выполнять действия в Steam
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

       

        static void OnProfileInfo(SteamFriends.ProfileInfoCallback callback)
        {

        }
        static void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            // получаем колличество друзей

            int friendCount = steamFriends.GetFriendCount();

            Console.WriteLine("We have {0} friends", friendCount);

            for (int x = 0; x < friendCount; x++)
            {

                steamIdFriend = steamFriends.GetFriendByIndex(x);

            }
        }

        static void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            // Проверяем, что это сообщение от Dota 2 GC
            if (callback.EMsg == (uint)EDOTAGCMsg.k_EMsgGCMatchDetailsResponse)
            {
                // Десериализуем ответ
                var response = new ClientGCMsgProtobuf<CMsgDOTALeagueList>(callback.Message);

                Console.WriteLine($"✅ Получены данные о матче! Match ID: {response.Body.leagues}");
            }
        }


    }
}
