DO $$

BEGIN

-- Schemas
CREATE SCHEMA IF NOT EXISTS gitclub;

-- Sequences
CREATE SEQUENCE IF NOT EXISTS gitclub.organization_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS gitclub.user_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;
    
CREATE SEQUENCE IF NOT EXISTS gitclub.team_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;
    
CREATE SEQUENCE IF NOT EXISTS gitclub.issue_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;
    
CREATE SEQUENCE IF NOT EXISTS gitclub.repository_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS gitclub.user_organization_role_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;
    
CREATE SEQUENCE IF NOT EXISTS gitclub.user_team_role_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;
    
CREATE SEQUENCE IF NOT EXISTS gitclub.user_repository_role_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;    

CREATE SEQUENCE IF NOT EXISTS gitclub.user_issue_role_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;    

CREATE SEQUENCE IF NOT EXISTS gitclub.team_repository_role_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS gitclub.outbox_event_seq
    start 38187
    increment 1
    NO MAXVALUE
    CACHE 1;

-- Tables
CREATE TABLE IF NOT EXISTS gitclub.user (
    user_id integer default nextval('gitclub.user_seq'),
    email varchar(2000) not null,
    preferred_name varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT user_pkey
        PRIMARY KEY (user_id),
    CONSTRAINT user_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.organization_role (
    organization_role_id integer,
    name varchar(255) not null,
    description varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT organization_role_pkey
        PRIMARY KEY (organization_role_id),
    CONSTRAINT organization_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.repository_role (
    repository_role_id integer,
    name varchar(255) not null,
    description varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT repository_role_pkey
        PRIMARY KEY (repository_role_id),
    CONSTRAINT repository_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.issue_role (
    issue_role_id integer,
    name varchar(255) not null,
    description varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT issue_role_pkey
        PRIMARY KEY (issue_role_id),
    CONSTRAINT issue_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.team_role (
    team_role_id integer,
    name varchar(255) not null,
    description varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT team_role_pkey
        PRIMARY KEY (team_role_id),
    CONSTRAINT team_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.base_repository_role (
    base_repository_role_id integer,
    name varchar(255) not null,
    description varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT base_repository_role_pkey
        PRIMARY KEY (base_repository_role_id),
    CONSTRAINT base_repository_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.organization (
    organization_id integer default nextval('gitclub.organization_seq'),
    name varchar(255) not null,
    base_repository_role_id integer not null,
    billing_address text null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT organization_pkey
        PRIMARY KEY (organization_id),
    CONSTRAINT organization_base_repository_role_id_fkey 
        FOREIGN KEY (base_repository_role_id)
        REFERENCES gitclub.base_repository_role(base_repository_role_id),
    CONSTRAINT organization_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.team (
    team_id integer default nextval('gitclub.team_seq'),
    organization_id integer not null,    
    name varchar(2000) not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT team_pkey
        PRIMARY KEY (team_id),
    CONSTRAINT team_organization_id_fkey 
        FOREIGN KEY (organization_id)
        REFERENCES gitclub.organization(organization_id),
    CONSTRAINT team_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.repository (
    repository_id integer default nextval('gitclub.repository_seq'),
    name varchar(255) not null,
    organization_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT repository_pkey
        PRIMARY KEY (repository_id),
    CONSTRAINT repository_organization_id_fkey 
        FOREIGN KEY (organization_id)
        REFERENCES gitclub.organization(organization_id),
    CONSTRAINT repository_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.issue (
    issue_id integer default nextval('gitclub.issue_seq'),
    title varchar(2000) not null,
    content text not null,
    closed boolean default false,
    repository_id integer not null,
    created_by integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT issue_pkey
        PRIMARY KEY (issue_id),
    CONSTRAINT issue_repository_id_fkey 
        FOREIGN KEY (repository_id)
        REFERENCES gitclub.repository(repository_id),
    CONSTRAINT issue_created_by_fkey 
        FOREIGN KEY (created_by)
        REFERENCES gitclub.user(user_id),
    CONSTRAINT issue_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.user_organization_role (
    user_organization_role_id integer default nextval('gitclub.user_organization_role_seq'),
    user_id integer not null,
    organization_id integer not null,
    organization_role_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT user_organization_role_pkey
        PRIMARY KEY (user_organization_role_id),
    CONSTRAINT user_organization_role_user_id_fkey 
        FOREIGN KEY (user_id)
        REFERENCES gitclub.user(user_id),
    CONSTRAINT user_organization_role_organization_role_id_fkey 
        FOREIGN KEY (organization_role_id)
        REFERENCES gitclub.organization_role(organization_role_id),
    CONSTRAINT user_organization_role_organization_id_fkey 
        FOREIGN KEY (organization_id)
        REFERENCES gitclub.organization(organization_id),
    CONSTRAINT user_organization_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.user_team_role (
    user_team_role_id integer default nextval('gitclub.user_team_role_seq'),
    user_id integer not null,
    team_id integer not null,
    team_role_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT user_team_role_pkey
        PRIMARY KEY (user_team_role_id),
    CONSTRAINT user_team_role_user_id_fkey 
        FOREIGN KEY (user_id)
        REFERENCES gitclub.user(user_id),
    CONSTRAINT user_team_role_team_role_id_fkey 
        FOREIGN KEY (team_role_id)
        REFERENCES gitclub.team_role(team_role_id),
    CONSTRAINT user_team_role_team_id_fkey 
        FOREIGN KEY (team_id)
        REFERENCES gitclub.team(team_id),
    CONSTRAINT user_team_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.user_repository_role (
    user_repository_role_id integer default nextval('gitclub.user_repository_role_seq'),
    user_id integer not null,
    repository_id integer not null,
    repository_role_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT user_repository_role_pkey
        PRIMARY KEY (user_repository_role_id),
    CONSTRAINT user_repository_role_user_id_fkey 
        FOREIGN KEY (user_id)
        REFERENCES gitclub.user(user_id),
    CONSTRAINT user_repository_role_repository_role_id_fkey 
        FOREIGN KEY (repository_role_id)
        REFERENCES gitclub.repository_role(repository_role_id),
    CONSTRAINT user_repository_role_repository_id_fkey 
        FOREIGN KEY (repository_id)
        REFERENCES gitclub.repository(repository_id),
    CONSTRAINT user_repository_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.user_issue_role (
    user_issue_role_id integer default nextval('gitclub.user_issue_role_seq'),
    user_id integer not null,
    issue_id integer not null,
    issue_role_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT user_issue_role_pkey
        PRIMARY KEY (user_issue_role_id),
    CONSTRAINT user_issue_role_user_id_fkey 
        FOREIGN KEY (user_id)
        REFERENCES gitclub.user(user_id),
    CONSTRAINT user_issue_role_repository_role_id_fkey 
        FOREIGN KEY (issue_role_id)
        REFERENCES gitclub.issue_role(issue_role_id),
    CONSTRAINT user_issue_role_issue_id_fkey 
        FOREIGN KEY (issue_id)
        REFERENCES gitclub.issue(issue_id),
    CONSTRAINT user_issue_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.team_repository_role (
    team_repository_role_id integer default nextval('gitclub.team_repository_role_seq'),
    team_id integer not null,
    repository_id integer not null,
    repository_role_id integer not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT team_repository_role_pkey
        PRIMARY KEY (team_repository_role_id),
    CONSTRAINT team_repository_role_team_id_fkey 
        FOREIGN KEY (team_id)
        REFERENCES gitclub.team(team_id),
    CONSTRAINT team_repository_role_repository_role_id_fkey 
        FOREIGN KEY (repository_role_id)
        REFERENCES gitclub.repository_role(repository_role_id),
    CONSTRAINT team_repository_role_repository_id_fkey 
        FOREIGN KEY (repository_id)
        REFERENCES gitclub.repository(repository_id),
    CONSTRAINT team_repository_role_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

CREATE TABLE IF NOT EXISTS gitclub.outbox_event (
    outbox_event_id integer default nextval('gitclub.outbox_event_seq'),
    correlation_id_1 varchar(2000) null,
    correlation_id_2 varchar(2000) null,
    correlation_id_3 varchar(2000) null,
    correlation_id_4 varchar(2000) null,
    event_time timestamptz not null,
    event_source varchar(2000) not null,
    event_type varchar(255) not null,
    payload JSONB not null,
    last_edited_by integer not null,
    sys_period tstzrange not null default tstzrange(current_timestamp, null),
    CONSTRAINT outbox_event_pkey
        PRIMARY KEY (outbox_event_id),
    CONSTRAINT outbox_event_last_edited_by_fkey 
        FOREIGN KEY (last_edited_by)
        REFERENCES gitclub.user(user_id)
);

-- Indexes
CREATE UNIQUE INDEX IF NOT EXISTS organization_name_key 
    ON gitclub.organization(name);

CREATE UNIQUE INDEX IF NOT EXISTS user_email_key 
    ON gitclub.user(email);

CREATE UNIQUE INDEX IF NOT EXISTS repository_organization_id_name_key 
    ON gitclub.repository(name, organization_id);

CREATE UNIQUE INDEX IF NOT EXISTS repository_role_name_key 
    ON gitclub.repository_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS team_role_name_key 
    ON gitclub.team_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS issue_role_name_key 
    ON gitclub.issue_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS user_team_role_user_id_team_id_key 
    ON gitclub.user_team_role(user_id, team_id);

CREATE UNIQUE INDEX IF NOT EXISTS user_repository_role_user_id_repository_id_key 
    ON gitclub.user_repository_role(user_id, repository_id);

CREATE UNIQUE INDEX IF NOT EXISTS user_organization_role_user_id_organization_id_key 
    ON gitclub.user_organization_role(user_id, organization_id);

CREATE UNIQUE INDEX IF NOT EXISTS team_repository_role_team_id_repository_id_key 
    ON gitclub.team_repository_role(team_id, repository_id);

CREATE UNIQUE INDEX IF NOT EXISTS organization_role_name_key 
    ON gitclub.organization_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS base_repository_role_name_key 
    ON gitclub.base_repository_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS team_organization_id_name_key 
    ON gitclub.team(organization_id, name);
	
END;
$$ LANGUAGE plpgsql;
