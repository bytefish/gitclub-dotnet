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

-- History Tables
CREATE TABLE IF NOT EXISTS gitclub.organization_history (
    LIKE gitclub.organization
);

CREATE TABLE IF NOT EXISTS gitclub.team_history (
    LIKE gitclub.team
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

CREATE TABLE IF NOT EXISTS gitclub.issue_role_history (
    LIKE gitclub.issue_role
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

CREATE TABLE IF NOT EXISTS gitclub.user_team_role_history (
    LIKE gitclub.user_team_role
);

CREATE TABLE IF NOT EXISTS gitclub.user_issue_role_history (
    LIKE gitclub.user_issue_role
);

CREATE TABLE IF NOT EXISTS gitclub.team_repository_role_history (
    LIKE gitclub.team_repository_role
);

CREATE TABLE IF NOT EXISTS gitclub.outbox_event_history (
    LIKE gitclub.outbox_event
);

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

CREATE OR REPLACE TRIGGER issue_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.issue_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.issue_role_history', true
);

CREATE OR REPLACE TRIGGER user_issue_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user_issue_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.user_issue_role_history', true
);

CREATE OR REPLACE TRIGGER team_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.team
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.team_history', true
);

CREATE OR REPLACE TRIGGER team_repository_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.team_repository_role
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

CREATE OR REPLACE TRIGGER user_team_role_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.user_team_role
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.user_team_role_history', true
);

CREATE OR REPLACE TRIGGER outbox_event_versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON gitclub.outbox_event
FOR EACH ROW EXECUTE PROCEDURE gitclub.versioning(
  'sys_period', 'gitclub.outbox_event_history', true
);
