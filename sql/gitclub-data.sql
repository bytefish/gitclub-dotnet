DO $$

BEGIN

-- Initial Data
INSERT INTO gitclub.user(user_id, email, preferred_name, last_edited_by) 
    VALUES 
        (1, 'philipp@bytefish.de', 'Data Conversion User', 1)        
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

INSERT INTO gitclub.issue_role(issue_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Creator', 'Creator Role on Issue', 1), 
        (2, 'Assignee', 'Assignee Role on Issue', 1),
        (3, 'Owner', 'Owner Role on Issue', 1),
        (4, 'Reader', 'Reader Role on Issue', 1),
        (5, 'Writer', 'Writer Role on Issue', 1)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.organization_role(organization_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Member', 'Member Role on Organization', 1), 
        (2, 'BillingManager', 'BillingManager Role on Organization', 1),
        (3, 'Owner', 'Owner Role on Organization', 1),
        (4, 'Administrator', 'Administrator Role on Organization', 1)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.team_role(team_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'Member', 'Member Role on Team', 1),
        (2, 'Maintainer', 'Maintainer Role on Team', 1), 
        (3, 'Owner', 'Owner Role on Team', 1) 
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.base_repository_role(base_repository_role_id, name, description, last_edited_by) 
    VALUES 
        (1, 'RepositoryReader', 'Automatically Reader on Repositories', 1),
        (2, 'RepositoryWriter', 'Automatically Writer on Repositories', 1),
        (3, 'RepositoryAdministrator', 'Automatically Administrator on Repositories', 1) 
    ON CONFLICT DO NOTHING;

-- OpenFGA GitHub Permissions Example (https://openfga.dev/docs/modeling/advanced/github)
INSERT INTO gitclub.user(user_id, email, preferred_name, last_edited_by) 
    VALUES 
        (2, 'anne@git.local', 'Anne', 1),
        (3, 'beth@git.local', 'Beth', 1),
        (4, 'charles@git.local', 'Charles', 1),
        (5, 'diane@git.local', 'Diane', 1),
        (6, 'erik@git.local', 'Eril', 1)        
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.organization(organization_id, name, base_repository_role_id, billing_address, last_edited_by) 
    VALUES 
        (1, 'Contoso', 3, 'ACME Street. 93', 1) 
    ON CONFLICT DO NOTHING; 

INSERT INTO gitclub.team(team_id, organization_id, name, last_edited_by) 
    VALUES 
        (1, 1, 'Engineering', 1),    -- A Team "Engineering" (1), that belongs to the "contoso" Organization (1)
        (2, 1, 'Protocols', 1)       -- A Team "Protocols" (1), that belongs to the "contoso" Organization (1)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.repository(repository_id, organization_id, name, last_edited_by)
    VALUES
        (1, 1, 'Tooling', 1),    		-- A Repository "Tooling" (1), that belongs to the "Contoso" (1) organization
        (2, 1, 'Specifications', 1)		-- A Repository "Specifications" (2), that belongs to the "Contoso" (1) organization
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.issue(issue_id, title, content, closed, repository_id, created_by, last_edited_by)
    VALUES
        (1, 'GitClub rocks!', 'Amazing Project!', false, 1, 1, 1)
    ON CONFLICT DO NOTHING;


INSERT INTO gitclub.user_team_role(user_team_role_id, user_id, team_id, team_role_id, last_edited_by)
    VALUES
        (1, 4, 1, 2, 1),    -- Charles (4) is a Member (2) or Team Contoso/Engineering (1)
        (2, 5, 2, 2, 1)     -- Diane (5) is a Member (2) or Team Contoso/Protocols (2)
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.user_organization_role(user_organization_role_id, user_id, organization_id, organization_role_id, last_edited_by)
    VALUES
        (1, 1, 1, 3, 1),  -- Philipp (1) is the Owner (3) of Contoso (1) 
        (1, 6, 1, 1, 1)  -- Erik (6) is a Member (1) of Contoso (1),
    ON CONFLICT DO NOTHING;

INSERT INTO gitclub.user_repository_role(user_repository_role_id, user_id, repository_id, repository_role_id, last_edited_by)
    VALUES
        (1, 6, 1, 1, 1),    -- User "anne" (2) is a Reader (1) of the Repository "tooling" (1)
        (2, 6, 1, 3, 1)     -- User "beth" (2) is a Writer (3) of the Repository "tooling" (1)
    ON CONFLICT DO NOTHING;

END;
$$ LANGUAGE plpgsql;
