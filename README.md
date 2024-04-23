# GitClub with .NET and OpenFGA #

This is an example application based on GitHub, that's meant to model GitHub's permissions system using OpenFGA.

It's modeled after the GitClub example provided by the Oso team at:

* [https://github.com/osohq/gitclub](https://github.com/osohq/gitclub)

You can start OpenFGA and the Postgres database using Docker Compose:

```
docker compose up
```

It will spin up a Postgres database with the GitClub Schema, and an OpenFGA Server with a GitHub-like Authorization model. 

The repository is still a work in progress and documentation is going to follow. 
