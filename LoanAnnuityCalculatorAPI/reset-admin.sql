-- Delete existing admin user
DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE UserName = 'admin');
DELETE FROM AspNetUsers WHERE UserName = 'admin';
