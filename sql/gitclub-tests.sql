
-- Performs a Cleanup for Tests, which removes all data except the Data Conversion User
CREATE OR REPLACE PROCEDURE gitclub.cleanup_tests()
AS $cleanup_tests_func$
BEGIN

	-- Delete all non-fixed data
	DELETE FROM gitclub.user_organization_role;
	DELETE FROM gitclub.user_team_role;
	DELETE FROM gitclub.user_repository_role;
	DELETE FROM gitclub.team_repository_role;
	
	DELETE FROM gitclub.issue;
	DELETE FROM gitclub.repository;
	DELETE FROM gitclub.team;
	DELETE FROM gitclub.organization;
	DELETE FROM gitclub.outbox_event;
	DELETE FROM gitclub.user WHERE user_id != 1;
	
	-- Delete historic data
	DELETE FROM gitclub.user_organization_role_history;
	DELETE FROM gitclub.user_team_role_history;
	DELETE FROM gitclub.user_issue_role_history;
	DELETE FROM gitclub.user_repository_role_history;
	DELETE FROM gitclub.team_repository_role_history;
	DELETE FROM gitclub.issue_history;
	DELETE FROM gitclub.repository_history;
	DELETE FROM gitclub.team_history;
	DELETE FROM gitclub.organization_history;
	DELETE FROM gitclub.user_history WHERE user_id != 1;
	
    
END; $cleanup_tests_func$ 
LANGUAGE plpgsql;
