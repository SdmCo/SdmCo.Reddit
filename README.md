# SdmCo Reddit

## Description

This application will monitor a provided list of subreddits for new posts and provide users with the following statistics for a given subreddit:

- Most upvoted post
- User with the most posts

## Pre-Requisites

- .NET 7
- Docker (for local Redis instance)
- A registered reddit applicaiton. [Register Here](https://www.reddit.com/wiki/api/)

## Setup

1. Set Environment Variables (You may use the provided setup_env scripts if desired)

- **Windows**:

```powershell
$env:RedditAuth_ClientId = "YOUR_CLIENT_ID"
$env:RedditAuth_ClientSecret = "YOUR_CLIENT_SECRET"
$env:RedditAuth_Username = "YOUR_USERNAME"
$env:RedditAuth_Password = "YOUR_PASSWORD"
```

- **Linux**:

```bash
export RedditAuth_ClientId="YOUR_CLIENT_ID"
export RedditAuth_ClientSecret="YOUR_CLIENT_SECRET"
export RedditAuth_Username="YOUR_USERNAME"
export RedditAuth_Password="YOUR_PASSWORD"
```

2. Create Redis Docker Container

- **Windows and Linux**

```text
cd infrastructure
docker-compose up -d
```

3. Configure Subreddit Worker Project

Open the **appsettings.json** file in **src/SdmCo.Reddit.Montior**:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  },
  "Subreddits": ["aww", "funny"],
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

Here you can modify the **Subreddits** list to include what subreddits you want to monitor.  If you did not modify the docker-compose file when creating the Redis container, you can leave the Redis connection string property alone.  If you did change the port, you will need to update it here as well as in the **appsettings.json** file in the **SdmCo.Reddit.Api** project.

## Running The Application

### Visual Studio

If you are using Visual Studio, make sure both the Api and Monitor projects are selected as start up projects in the solution.

### Console

If you are not using Visual Studio, you can do the following:

1. **Restore NuGet Packages**

   Navigate to the solution directory and run the following command to restore all NuGet packages for the solution.

   ```bash
   dotnet restore
   ```

2. **Run the API Project**

   Navigate to the Api project directory (`src/SdmCo.Reddit.Api`) and execute the following command:

   ```bash
   dotnet run
   ```

   This will start the Api project.  Keep this terminal window open.  In the logs you will see a URL the Api project is listening on.  You can browse to `http://localhost:port/swagger/` to test the Api.

3. **Run the Monitor Project**

   Open a new terminal and navigate to the Worker project directory (`src/SdmCo.Reddit.Monitor`).  Execute the following command:

   ```bash
   dotnet run
   ```

   This will start the Monitor project.  Keep this terminal window open.  You should see the monitor project begin to request data from reddit upon start.

## Accessing Subreddit Statistics

The Monitor project will spawn a new task for each subreddit you defined in its **appsettings.json** file.  After the first time a subreddit is checked for new posts, there will be an exponential delay between subsequent requests if no new posts are found.  This will keep the applicaiton from spamming the reddit servers trying to get new posts that do not exist.  If after 5 attempts there are no new posts, the exponential delay will max out at 32 seconds **(2^5)** and remain at that rate until new posts are found at which point the delay will reset to 0 seconds **(2^0)** and the exponential delay process will begin again.

The first request to pull new posts from each subreddit will return data, so you can query the statistics right away.  You may use the provided swagger UI to query for subreddit statistics directly, or use another REST client/tool.  Making a **GET request** to the **/Statistics/{subreddit name}** endpoint will return the following response:

```json
{
  "subredditName": "string",
  "mostUpvotedPost": {
    "postTitle": "string",
    "upVotes": 0
  },
  "userWithMostPosts": {
    "username": "string",
    "postCount": 0
  }
}
```