SELECT u.Id, u.UserName, u.Email, u.IsActive, u.FirstName, u.LastName, r.Name as Role
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.UserName = 'admin';
