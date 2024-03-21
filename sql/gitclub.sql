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

CREATE SEQUENCE IF NOT EXISTS gitclub.team_repository_role_seq
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

-- Indexes
CREATE UNIQUE INDEX IF NOT EXISTS organization_name_key 
    ON gitclub.organization(name);

CREATE UNIQUE INDEX IF NOT EXISTS user_email_key 
    ON gitclub.user(email);
    
CREATE UNIQUE INDEX IF NOT EXISTS repository_name_organization_id_key 
    ON gitclub.repository(name, organization_id);
    
CREATE UNIQUE INDEX IF NOT EXISTS repository_role_name_key 
    ON gitclub.repository_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS team_role_name_key 
    ON gitclub.team_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS organization_role_name_key 
    ON gitclub.organization_role(name);

CREATE UNIQUE INDEX IF NOT EXISTS base_repository_role_name_key 
    ON gitclub.base_repository_role(name);

-- History Tables
CREATE TABLE IF NOT EXISTS gitclub.organization_history (
    LIKE gitclub.organization
);

CREATE TABLE IF NOT EXISTS gitclub.user_history (
    LIKE gitclub.user
);

CREATE TABLE IF NOT EXISTS gitclub.organization_role_history (
    LIKE gitclub.organization_role
);

CREATE TABLE IF NOT EXISTS gitclub.repository_role_history (
    LIKE gitclub.repository_role
);

CREATE TABLE IF NOT EXISTS gitclub.team_role_history (
    LIKE gitclub.team_role
);

CREATE TABLE IF NOT EXISTS gitclub.base_repository_role_history (
    LIKE gitclub.base_repository_role
);

CREATE TABLE IF NOT EXISTS gitclub.repository_history (
    LIKE gitclub.repository
);

CREATE TABLE IF NOT EXISTS gitclub.issue_history (
    LIKE gitclub.issue
);

CREATE TABLE IF NOT EXISTS gitclub.user_organization_role_history (
    LIKE gitclub.user_organization_role
);

CREATE TABLE IF NOT EXISTS gitclub.user_repository_role_history (
    LIKE gitclub.user_repository_role
);

CREATE TABLE IF NOT EXISTS gitclub.team_repository_role_history (
    LIKE gitclub.team_repository_role
);

-- Versioning Function (https://github.com/nearform/temporal_tables)
CREATE OR REPLACE FUNCTION gitclub.versioning()
RETURNS TRIGGER AS $versioning_func$
DECLARE
  sys_period text;
  history_table text;
  manipulate jsonb;
  ignore_unchanged_values bool;
  commonColumns text[];
  time_stamp_to_use timestamptz;
  range_lower timestamptz;
  existing_range tstzrange;
  holder record;
  holder2 record;
  pg_version integer;
  newVersion record;
  oldVersion record;
  user_defined_system_time text;
BEGIN
  -- set custom system time if exists
  BEGIN
    SELECT current_setting('user_defined.system_time') INTO user_defined_system_time;
    IF NOT FOUND OR (user_defined_system_time <> '') IS NOT TRUE THEN
      time_stamp_to_use := CURRENT_TIMESTAMP;
    ELSE
      SELECT TO_TIMESTAMP(
          user_defined_system_time,
          'YYYY-MM-DD HH24:MI:SS.MS.US'
      ) INTO time_stamp_to_use;
    END IF;
    EXCEPTION WHEN OTHERS THEN
      time_stamp_to_use := CURRENT_TIMESTAMP;
  END;

  IF TG_WHEN != 'BEFORE' OR TG_LEVEL != 'ROW' THEN
    RAISE TRIGGER_PROTOCOL_VIOLATED USING
    MESSAGE = 'function "versioning" must be fired BEFORE ROW';
  END IF;

  IF TG_OP != 'INSERT' AND TG_OP != 'UPDATE' AND TG_OP != 'DELETE' THEN
    RAISE TRIGGER_PROTOCOL_VIOLATED USING
    MESSAGE = 'function "versioning" must be fired for INSERT or UPDATE or DELETE';
  END IF;

  IF TG_NARGS not in (3,4) THEN
    RAISE INVALID_PARAMETER_VALUE USING
    MESSAGE = 'wrong number of parameters for function "versioning"',
    HINT = 'expected 3 or 4 parameters but got ' || TG_NARGS;
  END IF;

  sys_period := TG_ARGV[0];
  history_table := TG_ARGV[1];
  ignore_unchanged_values := TG_ARGV[3];

  IF ignore_unchanged_values AND TG_OP = 'UPDATE' THEN
    IF NEW IS NOT DISTINCT FROM OLD THEN
      RETURN OLD;
    END IF;
  END IF;

  -- check if sys_period exists on original table
  SELECT atttypid, attndims INTO holder FROM pg_attribute WHERE attrelid = TG_RELID AND attname = sys_period AND NOT attisdropped;
  IF NOT FOUND THEN
    RAISE 'column "%" of relation "%" does not exist', sys_period, TG_TABLE_NAME USING
    ERRCODE = 'undefined_column';
  END IF;
  IF holder.atttypid != to_regtype('tstzrange') THEN
    IF holder.attndims > 0 THEN
      RAISE 'system period column "%" of relation "%" is not a range but an array', sys_period, TG_TABLE_NAME USING
      ERRCODE = 'datatype_mismatch';
    END IF;

    SELECT rngsubtype INTO holder2 FROM pg_range WHERE rngtypid = holder.atttypid;
    IF FOUND THEN
      RAISE 'system period column "%" of relation "%" is not a range of timestamp with timezone but of type %', sys_period, TG_TABLE_NAME, format_type(holder2.rngsubtype, null) USING
      ERRCODE = 'datatype_mismatch';
    END IF;

    RAISE 'system period column "%" of relation "%" is not a range but type %', sys_period, TG_TABLE_NAME, format_type(holder.atttypid, null) USING
    ERRCODE = 'datatype_mismatch';
  END IF;

  IF TG_OP = 'UPDATE' OR TG_OP = 'DELETE' THEN
    -- Ignore rows already modified in the current transaction
    IF OLD.xmin::text = (txid_current() % (2^32)::bigint)::text THEN
      IF TG_OP = 'DELETE' THEN
        RETURN OLD;
      END IF;

      RETURN NEW;
    END IF;

    SELECT current_setting('server_version_num')::integer
    INTO pg_version;

    -- to support postgres < 9.6
    IF pg_version < 90600 THEN
      -- check if history table exits
      IF to_regclass(history_table::cstring) IS NULL THEN
        RAISE 'relation "%" does not exist', history_table;
      END IF;
    ELSE
      IF to_regclass(history_table) IS NULL THEN
        RAISE 'relation "%" does not exist', history_table;
      END IF;
    END IF;

    -- check if history table has sys_period
    IF NOT EXISTS(SELECT * FROM pg_attribute WHERE attrelid = history_table::regclass AND attname = sys_period AND NOT attisdropped) THEN
      RAISE 'history relation "%" does not contain system period column "%"', history_table, sys_period USING
      HINT = 'history relation must contain system period column with the same name and data type as the versioned one';
    END IF;

    EXECUTE format('SELECT $1.%I', sys_period) USING OLD INTO existing_range;

    IF existing_range IS NULL THEN
      RAISE 'system period column "%" of relation "%" must not be null', sys_period, TG_TABLE_NAME USING
      ERRCODE = 'null_value_not_allowed';
    END IF;

    IF isempty(existing_range) OR NOT upper_inf(existing_range) THEN
      RAISE 'system period column "%" of relation "%" contains invalid value', sys_period, TG_TABLE_NAME USING
      ERRCODE = 'data_exception',
      DETAIL = 'valid ranges must be non-empty and unbounded on the high side';
    END IF;

    IF TG_ARGV[2] = 'true' THEN
      -- mitigate update conflicts
      range_lower := lower(existing_range);
      IF range_lower >= time_stamp_to_use THEN
        time_stamp_to_use := range_lower + interval '1 microseconds';
      END IF;
    END IF;

    WITH history AS
      (SELECT attname, atttypid
      FROM   pg_attribute
      WHERE  attrelid = history_table::regclass
      AND    attnum > 0
      AND    NOT attisdropped),
      main AS
      (SELECT attname, atttypid
      FROM   pg_attribute
      WHERE  attrelid = TG_RELID
      AND    attnum > 0
      AND    NOT attisdropped)
    SELECT
      history.attname AS history_name,
      main.attname AS main_name,
      history.atttypid AS history_type,
      main.atttypid AS main_type
    INTO holder
      FROM history
      INNER JOIN main
      ON history.attname = main.attname
    WHERE
      history.atttypid != main.atttypid;

    IF FOUND THEN
      RAISE 'column "%" of relation "%" is of type % but column "%" of history relation "%" is of type %',
        holder.main_name, TG_TABLE_NAME, format_type(holder.main_type, null), holder.history_name, history_table, format_type(holder.history_type, null)
      USING ERRCODE = 'datatype_mismatch';
    END IF;

    WITH history AS
      (SELECT attname
      FROM   pg_attribute
      WHERE  attrelid = history_table::regclass
      AND    attnum > 0
      AND    NOT attisdropped),
      main AS
      (SELECT attname
      FROM   pg_attribute
      WHERE  attrelid = TG_RELID
      AND    attnum > 0
      AND    NOT attisdropped)
    SELECT array_agg(quote_ident(history.attname)) INTO commonColumns
      FROM history
      INNER JOIN main
      ON history.attname = main.attname
      AND history.attname != sys_period;
    -- skip version if it would be identical to the previous version
    IF ignore_unchanged_values AND TG_OP = 'UPDATE' AND array_length(commonColumns, 1) > 0 THEN
      EXECUTE 'SELECT ROW($1.' || array_to_string(commonColumns , ', $1.') || ')'
        USING NEW
        INTO newVersion;
      EXECUTE 'SELECT ROW($1.' || array_to_string(commonColumns , ', $1.') || ')'
        USING OLD
        INTO oldVersion;
      IF newVersion IS NOT DISTINCT FROM oldVersion THEN
        RETURN NEW;
      END IF;
    END IF;
    EXECUTE ('INSERT INTO ' ||
      history_table ||
      '(' ||
      array_to_string(commonColumns , ',') ||
      ',' ||
      quote_ident(sys_period) ||
      ') VALUES ($1.' ||
      array_to_string(commonColumns, ',$1.') ||
      ',tstzrange($2, $3, ''[)''))')
       USING OLD, range_lower, time_stamp_to_use;
  END IF;

  IF TG_OP = 'UPDATE' OR TG_OP = 'INSERT' THEN
    manipulate := jsonb_set('{}'::jsonb, ('{' || sys_period || '}')::text[], to_jsonb(tstzrange(time_stamp_to_use, null, '[)')));

    RETURN jsonb_populate_record(NEW, manipulate);
  END IF;

  RETURN OLD;
END;
$versioning_func$ LANGUAGE plpgsql;

-- Triggers (Versioning)
CREATE OR REPLACE TRIGGER organization_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.organization
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.organization_history', true
);

CREATE OR REPLACE TRIGGER organization_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.organization_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.organization_role_history', true
);

CREATE OR REPLACE TRIGGER repository_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.repository_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.repository_role_history', true
);

CREATE OR REPLACE TRIGGER team_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.team_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.team_role_history', true
);

CREATE OR REPLACE TRIGGER base_repository_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.base_repository_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.base_repository_role_history', true
);

CREATE OR REPLACE TRIGGER user_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.user_history', true
);

CREATE OR REPLACE TRIGGER repository_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.repository
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.repository_history', true
);

CREATE OR REPLACE TRIGGER issue_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.issue
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.issue_history', true
);

CREATE OR REPLACE TRIGGER team_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.team
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.team_history', true
);

CREATE OR REPLACE TRIGGER team_repository_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user_organization_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.team_repository_role_history', true
);

CREATE OR REPLACE TRIGGER user_organization_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user_organization_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.user_organization_role_history', true
);

CREATE OR REPLACE TRIGGER user_repository_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user_repository_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.user_repository_role_history', true
);

-- Initial data
INSERT INTO gitclub.user(user_id, email, preferred_name, last_edited_by) 
    VALUES 
        (1, 'philipp@bytefish.de', 'Data Conversion User', 1),
        (2, 'anne@git.local', 'Anne', 1),
        (3, 'beth@git.local', 'Beth', 1),
        (4, 'charles@git.local', 'Charles', 1),
        (5, 'diane@git.local', 'Diane', 1),
        (6, 'erik@git.local', 'Eril', 1)        
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.repository_role(repository_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Reader', 'Reader Role on Repository', 1), 
        (2, 'Triager', 'Triager Role on Repository', 1),
        (3, 'Writer', 'Writer Role on Repository', 1), 
        (4, 'Maintainer', 'Maintainer Role on Repository', 1),
        (5, 'Administrator', 'Administrator Role on Repository', 1),
        (6, 'Owner', 'Owner Role on Repository', 1)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.organization_role(organization_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Member', 'Member Role on Organization', 1), 
        (2, 'BillingManager', 'BillingManager Role on Organization', 1),
        (3, 'Owner', 'Owner Role on Organization', 1),
        (4, 'Administrator', 'Administrator Role on Organization', 1),
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.team_role(team_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Member', 'Member Role on Team', 1),
        (2, 'Maintainer', 'Maintainer Role on Team', 1) 
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.base_repository_role(base_repository_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'RepositoryReader', 'Automatically Reader on Repositories', 1),
        (2, 'RepositoryWriter', 'Automatically Writer on Repositories', 1),
        (3, 'RepositoryAdministrator', 'Automatically Administrator on Repositories', 1) 
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.organization(organization_id, name, base_repository_role_id, billing_address, last_edited_by) 
    VALUES 
        (1, 'contoso', 3, 'ACME Street. 93', 1) 
    ON CONFLICT DO NOTHING; 

INSERT INTO gitclub.team(team_id, organization_id, name, last_edited_by) 
    VALUES 
        (1, 1, 'engineering', 1),    -- A Team "Engineering" (1), that belongs to the "contoso" Organization (1)
        (2, 1, 'Protocols', 1)       -- A Team "Protocols" (1), that belongs to the "contoso" Organization (1)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.repository(repository_id, organization_id, name, last_edited_by)
    VALUES
        (1, 1, 'tooling', 1)    -- A Repository "tooling" (1), that belongs to the "contoso" (1) organization
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.user_team_role(user_team_role_id, user_id, team_id, team_role_id, last_edited_by)
    VALUES
        (1, 4, 1, 2, 1),    -- Charles (4) is a Member (2) or Team contoso/engineering (1)
        (2, 5, 2, 2, 1)     -- Diane (5) is a Member (2) or Team contoso/protocols (2)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.user_organization_role(user_organization_role_id, user_id, organization_id, organization_role_id, last_edited_by)
    VALUES
        (1, 6, 1, 1, 1) -- Erik (6) is a Member (1) of contoso (1)User 
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.user_repository_role(user_repository_role_id, user_id, repository_id, repository_role_id, last_edited_by)
    VALUES
        (1, 6, 1, 1, 1),    -- User "anne" (2) is a Reader (1) of the Repository "tooling" (1)
        (2, 6, 1, 3, 1)     -- User "beth" (2) is a Writer (3) of the Repository "tooling" (1)
    ON CONFLICT DO NOTHING;

END;
$$ LANGUAGE plpgsql;