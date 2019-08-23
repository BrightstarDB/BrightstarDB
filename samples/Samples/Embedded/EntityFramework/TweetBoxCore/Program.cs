using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using TweetBoxCore;

namespace BrightstarDB.Samples.EntityFramework.TweetBoxCore
{
    /// <summary>
    /// This console application shows how to quickly get started with BrightstarDB
    /// More information about this sample project is in the documentation located at [[INSTALLERDIR]]\Docs
    /// </summary>
    class Program
    {
        private static void Main(string[] args)
        {
            // Initialise license and stores directory location
            SamplesConfiguration.Register();

            //create a unique store name
            var storeName = "tweetbox_" + Guid.NewGuid().ToString();

            //connection string to the BrightstarDB service
            string connectionString = string.Format(@"Type=embedded;storesDirectory={0};StoreName={1};",
                                                    SamplesConfiguration.StoresDirectory,
                                                    storeName);

            const int numUsers = 100;

            //Load the database with users, hashtags and tweets
            var context = new EntityContext(connectionString);
            var timetaken = LoadTweetBoxContent(context, numUsers);
            const int numTweets = numUsers * 200;

            Console.WriteLine("Created, persisted and indexed {0} users, {1} tweets, {2} hashtags in {3} seconds.",
                              numUsers, numTweets, _allHashTags.Count, (timetaken / 1000));

            // Shutdown Brightstar processing threads.
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        }

        static readonly Random Random = new Random();
        private static List<IUser> _allUsers;
        private static List<IHashTag> _allHashTags;
        private static int _numFollowing;

        /// <summary>
        /// This method adds all the data into the system
        /// </summary>
        /// <param name="context"></param>
        /// <param name="numUsers">The number of users to add to the system</param>
        /// <returns>The time taken in milliseconds that the method took to complete</returns>
        public static double LoadTweetBoxContent(EntityContext context, int numUsers)
        {
            var sw = new Stopwatch();
            sw.Start();

            //Add users and hashtags to the system
            _allUsers = AddUsers(context, numUsers);
            _allHashTags = AddHashTags(context);
            Console.WriteLine(@"Saving users and hash tags");
            //Save this base data to the database
            context.SaveChanges();

            //loop through the set of users created, and add followers and tweets to each user
            foreach (var user in _allUsers)
            {
                AddFollowers(context, user);
                AddTweets(context, user, 200);
            }
            Console.WriteLine(@"Saving changes");
            context.SaveChanges();

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Add a set of users to the system
        /// </summary>
        /// <param name="context"></param>
        /// <param name="numUsers">The number of users to add</param>
        /// <returns></returns>
        public static List<IUser> AddUsers(EntityContext context, int numUsers)
        {
            Console.WriteLine(@"Adding " + numUsers + " users");
            var users = new List<IUser>();
            for (var i = 0; i < numUsers; i++)
            {
                //create a random username
                var r1 = Random.Next(0, 24);
                var r2 = Random.Next(0, 24);
                var r3 = Random.Next(1, 2000);
                var username = Firstnames[r1] + Surnames[r2] + r3.ToString();

                //create a user
                var user = context.Users.Create();
                //set the properties
                user.Username = username;
                user.Bio = Sentence(100);
                user.DateRegistered = GetRandomRegistrationDate();

                var sna = new SocialNetworkAccount { AccountName = Word(15), AuthorizationKey = Word(26) };
                context.SocialNetworkAccounts.Add(sna);
                user.SocialNetworkAccount = sna;

                //keep track of all users to return
                users.Add(user);
            }
            return users;
        }

        /// <summary>
        /// Add a set of hashtags to the system
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<IHashTag> AddHashTags(EntityContext context)
        {
            Console.WriteLine(@"Adding 10,000 hashtags");
            var hashtags = new List<IHashTag>();
            for (var i = 0; i < 10000; i++)
            {
                //create a word
                var value = Word(6);
                //create a hashtag
                var hashtag = context.HashTags.Create();
                //set the properties
                hashtag.Value = value;
                //keep a track of all hashtags to return
                hashtags.Add(hashtag);
            }
            return hashtags;
        }

        /// <summary>
        /// Set 150 followers for a user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user">The user that the followers should be added to</param>
        public static void AddFollowers(EntityContext context, IUser user)
        {
            int totalNumberOfUsers = _allUsers.Count();
            _numFollowing = 75;
            if (totalNumberOfUsers < 75)
            {
                //if there are less users in the system than the amount of followers required, reduce the number to follow
                _numFollowing = totalNumberOfUsers - 1;
            }
            Console.WriteLine("Adding relationships to followers");
            for (var i = 0; i < _numFollowing; i++)
            {
                //get a user to follow
                var rdm = Random.Next(_allUsers.Count - 1);
                var userToFollow = _allUsers[rdm];
                if (userToFollow.Id.Equals(user.Id)) continue;
                user.Following.Add(userToFollow);
            }
        }

        /// <summary>
        /// Publish tweets (each including 2 hashtags) for a user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user">The user that is publishing the tweets</param>
        /// <param name="numTweets">The number of tweets to publish</param>
        public static void AddTweets(EntityContext context, IUser user, int numTweets)
        {
            Console.WriteLine(@"Adding " + numTweets + " tweets for user '" + user.Username + "'");
            //work out the interval between each tweet by taking the user's registration date and dividing by the number of tweets to publish
            var ticks = DateTime.Now.Subtract(user.DateRegistered).Ticks;
            var tweetGap = ticks / numTweets;

            //set the initial post date to the user's registration date
            var postDate = user.DateRegistered;
            for (int i = 0; i < numTweets; i++)
            {
                //the content of the tweet
                const string tweetcontent = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. In a nulla vitae nunc dictum dignissim sed ut ligula. Class aptent.";

                //get 2 random hashtags to include
                var htcount = _allHashTags.Count;
                var hti = Random.Next(0, htcount);
                var hti2 = Random.Next(0, htcount);
                var hashtag = _allHashTags[hti];
                var hashtag2 = _allHashTags[hti2];

                //create a tweet
                var tweet = context.Tweets.Create();
                //set the tweet's properties
                tweet.Content = tweetcontent;
                tweet.HashTags.Add(hashtag);
                tweet.HashTags.Add(hashtag2);
                tweet.DatePublished = postDate;
                tweet.Author = user;

                //increase postDate for the next tweet
                postDate = postDate.AddTicks(tweetGap);
            }
        }


        private static readonly List<string> Firstnames = new List<string>
                                 {
                                     "Jen",
                                     "Kal",
                                     "Gra",
                                     "Andy",
                                     "Jessica",
                                     "Adam",
                                     "Trevor",
                                     "Morris",
                                     "Paul",
                                     "Jane",
                                     "Elliot",
                                     "Annie",
                                     "Rob",
                                     "Mark",
                                     "Tim",
                                     "Gemma",
                                     "Clare",
                                     "Anna",
                                     "Tessa",
                                     "Julia",
                                     "David",
                                     "Andrew",
                                     "Charlie",
                                     "Aled",
                                     "Alex"
                                 };
        private static readonly List<string> Surnames = new List<string>
                                 {
                                     "Wilson",
                                     "Foster",
                                     "Green",
                                     "Fahy",
                                     "Goldsack",
                                     "Webb",
                                     "Fernley",
                                     "McKee",
                                     "Hughes",
                                     "Wong",
                                     "Sully",
                                     "Hague",
                                     "Boyce",
                                     "Pegeot",
                                     "Chappell",
                                     "East",
                                     "Tate",
                                     "Wade",
                                     "Lloyd",
                                     "Hopwseith",
                                     "Matthews",
                                     "Lacey",
                                     "Skipper",
                                     "Chandler",
                                     "Jones"
                                 };

        private static DateTime GetRandomRegistrationDate()
        {
            var day = Random.Next(1, 28);
            var month = Random.Next(1, 12);
            var year = Random.Next(2008, 2010);
            return new DateTime(year, month, day);
        }

        private static string Sentence(int size)
        {
            var sb = new StringBuilder();
            while (sb.Length < size)
            {
                var wordLength = Random.Next(1, 10);
                var word = Word(wordLength);
                sb.Append(word + " ");
            }

            return sb.ToString().Substring(0, size).Trim();
        }

        private static string Word(int size)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 65)));
                sb.Append(ch);
            }
            return sb.ToString().ToLower();
        }


    }
}
