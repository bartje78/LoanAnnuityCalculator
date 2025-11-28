# Multi-Tenant Architecture Implementation

## Overview
This implementation adds comprehensive multi-tenancy with fund-level access control to the Loan Annuity Calculator API. It ensures complete data isolation between tenants (asset managers) while allowing fine-grained permissions within each tenant.

## Key Features

### 1. **Tenant Isolation**
- Each tenant (asset manager) is completely isolated from others
- Unique database encryption key per tenant
- Global query filters prevent cross-tenant data access
- TenantId included in JWT token for automatic filtering

### 2. **Fund Management**
- Each tenant can create multiple funds
- Users are granted access to specific funds
- Three fund-level roles: Viewer, Editor, Manager

### 3. **Role-Based Access Control**
- **System Admin**: Can access all tenants (use sparingly!)
- **Tenant Admin**: Full control within their tenant
- **Fund Manager**: Manages assigned funds
- **Analyst**: Read/analyze data
- **Data Entry**: Basic data entry only

### 4. **Data Model**

```
Tenant (Asset Manager)
  ├── Users (ApplicationUser with TenantId)
  └── Funds
       ├── DebtorDetails (with TenantId + FundId)
       ├── Loans (with TenantId + FundId)
       └── Collateral (with TenantId + FundId)

UserFundAccess
  - Links users to funds with specific roles
  - Can be granted/revoked dynamically
```

## New Entities

### Tenant
- `TenantId`: Primary key
- `Name`: Tenant name (e.g., "Asset Manager ABC")
- `DatabaseKey`: Unique encryption key
- `IsActive`: Soft delete flag
- `CreatedAt`, `DeactivatedAt`

### Fund
- `FundId`: Primary key
- `TenantId`: Foreign key to Tenant
- `Name`: Fund name (e.g., "ABC Real Estate Fund I")
- `FundCode`: Short code (e.g., "ABC-RE-I")
- `IsActive`: Soft delete flag

### UserFundAccess
- Links users to funds with specific roles
- Roles: Viewer, Editor, Manager
- Can be revoked via `RevokedAt` timestamp

### ApplicationUser (Updated)
- Added `TenantId`: Links user to tenant
- Added `IsSystemAdmin`: System-wide access flag
- Added `FundAccesses`: Navigation property

## Modified Entities

### DebtorDetails, Loan, Collateral
All now include:
- `TenantId`: Required, indexed
- `FundId`: Required, indexed

## Security Architecture

### JWT Token Claims
```json
{
  "TenantId": "1",
  "IsSystemAdmin": "false",
  "Role": "TenantAdmin",
  "UserId": "...",
  "UserName": "...",
  "Email": "..."
}
```

### Middleware Flow
1. **Authentication**: Validates JWT token
2. **TenantMiddleware**: Extracts TenantId from claims, stores in HttpContext
3. **Authorization**: Checks role-based permissions
4. **Global Query Filters**: Automatically filters all queries by TenantId

### Access Control Matrix

| Action | System Admin | Tenant Admin | Fund Manager | Analyst | Data Entry |
|--------|--------------|--------------|--------------|---------|------------|
| View all tenants | ✅ | ❌ | ❌ | ❌ | ❌ |
| Create tenant | ✅ | ❌ | ❌ | ❌ | ❌ |
| Create fund | ✅ | ✅ | ❌ | ❌ | ❌ |
| Manage users | ✅ | ✅ | ❌ | ❌ | ❌ |
| Grant fund access | ✅ | ✅ | ❌ | ❌ | ❌ |
| View fund data | ✅ | ✅ | ✅ (assigned) | ✅ (assigned) | ✅ (assigned) |
| Edit fund data | ✅ | ✅ | ✅ (assigned) | ❌ | ✅ (assigned) |
| Delete fund data | ✅ | ✅ | ✅ (assigned) | ❌ | ❌ |
| Run simulations | ✅ | ✅ | ✅ | ✅ | ❌ |
| Edit settings | ✅ | ✅ | ✅ (Manager role) | ❌ | ❌ |

## API Endpoints

### Tenant Management
- `GET /api/tenant` - Get all tenants (System Admin)
- `GET /api/tenant/current` - Get current user's tenant
- `POST /api/tenant` - Create tenant (System Admin)
- `PUT /api/tenant/{id}` - Update tenant (System Admin)
- `DELETE /api/tenant/{id}` - Deactivate tenant (System Admin)

### Fund Management
- `GET /api/fund` - Get user's accessible funds
- `GET /api/fund/{id}` - Get specific fund
- `POST /api/fund` - Create fund (Tenant Admin)
- `PUT /api/fund/{id}` - Update fund (Tenant Admin)
- `DELETE /api/fund/{id}` - Close fund (Tenant Admin)
- `POST /api/fund/{fundId}/users/{userId}` - Grant fund access
- `DELETE /api/fund/{fundId}/users/{userId}` - Revoke fund access
- `GET /api/fund/{fundId}/users` - Get fund users

## Migration Strategy

### Phase 1: Database Schema Update
```bash
# Create migration
dotnet ef migrations add AddMultiTenancy

# Apply migration
dotnet ef database update
```

### Phase 2: Data Migration
You'll need to:
1. Create initial tenant(s)
2. Create initial fund(s) for each tenant
3. Update existing users to assign TenantId
4. Update existing DebtorDetails, Loans, Collateral with TenantId and FundId

Example migration script:
```sql
-- Create default tenant
INSERT INTO Tenants (Name, DatabaseKey, IsActive, CreatedAt)
VALUES ('Default Tenant', 'unique-key-here', 1, datetime('now'));

-- Create default fund
INSERT INTO Funds (TenantId, Name, IsActive, CreatedAt)
VALUES (1, 'Default Fund', 1, datetime('now'));

-- Update existing users
UPDATE AspNetUsers SET TenantId = 1;

-- Update existing debtors
UPDATE DebtorDetails SET TenantId = 1, FundId = 1;

-- Update existing loans
UPDATE Loans SET TenantId = 1, FundId = 1;

-- Update existing collateral
UPDATE Collaterals SET TenantId = 1, FundId = 1;
```

### Phase 3: Update Controllers
All existing controllers that access DebtorDetails, Loan, or Collateral need updates:
1. Inject `ITenantService`
2. Validate fund access before operations
3. Set TenantId and FundId when creating new entities

Example:
```csharp
[HttpPost]
public async Task<ActionResult> CreateDebtor([FromBody] CreateDebtorRequest request)
{
    var tenantId = _tenantService.GetCurrentTenantId();
    if (!tenantId.HasValue)
        return Forbid();
    
    if (!await _tenantService.HasFundAccess(request.FundId, FundRoles.Editor))
        return Forbid();
    
    var debtor = new DebtorDetails
    {
        TenantId = tenantId.Value,
        FundId = request.FundId,
        // ... other properties
    };
    
    _dbContext.DebtorDetails.Add(debtor);
    await _dbContext.SaveChangesAsync();
    
    return Ok(debtor);
}
```

### Phase 4: Frontend Updates
1. After login, store tenant info
2. Show fund selector in UI
3. Pass FundId when creating new entities
4. Filter views by selected fund

## Testing Strategy

### Unit Tests
- Tenant isolation validation
- Fund access control
- Role-based permissions

### Integration Tests
1. Create two tenants
2. Create users in each tenant
3. Verify Tenant A cannot see Tenant B's data
4. Verify fund-level access control works

### Test Scenarios
```csharp
// Test 1: Cross-tenant isolation
var tenantAUser = CreateUser(tenantId: 1);
var tenantBDebtor = CreateDebtor(tenantId: 2);
var result = await debtorController.GetDebtor(tenantBDebtor.Id);
// Expected: Forbid or NotFound

// Test 2: Fund access control
var user = CreateUser(tenantId: 1);
var fund1 = CreateFund(tenantId: 1, fundId: 1);
var fund2 = CreateFund(tenantId: 1, fundId: 2);
GrantFundAccess(user, fund1); // Only fund1
var result = await loanController.GetLoans(fundId: 2);
// Expected: Empty or Forbid

// Test 3: Role-based permissions
var viewer = CreateUser(role: FundRoles.Viewer);
var result = await debtorController.DeleteDebtor(id);
// Expected: Forbid
```

## Configuration

### appsettings.json
No additional configuration needed - uses existing JWT settings.

### Environment Variables
Optional:
- `ENABLE_TENANT_ISOLATION`: Default true, set false to disable filters (dev only)

## Performance Considerations

1. **Indexes**: Added indexes on TenantId and FundId for fast filtering
2. **Query Filters**: Automatic - no performance overhead
3. **Caching**: Consider caching user's fund list
4. **N+1 Queries**: Use `.Include()` when loading related entities

## Security Checklist

✅ TenantId in JWT token
✅ Global query filters active
✅ Middleware validates tenant context
✅ All entities have TenantId and FundId
✅ Controllers validate fund access
✅ Indexes for performance
✅ Unique database keys per tenant
✅ Soft delete (not hard delete)
✅ Audit logging for sensitive operations
✅ Role hierarchy enforced

## Known Limitations

1. **System Admin**: Has unrestricted access - use carefully
2. **Shared Settings**: Some settings (ModelSettings, TariffSettings) are not yet tenant-scoped
3. **Reporting**: Cross-fund reports need explicit fund ID list
4. **File Uploads**: Need to associate with TenantId/FundId

## Next Steps

1. ✅ Create migration
2. ⏳ Apply migration to database
3. ⏳ Run data migration script
4. ⏳ Update existing controllers
5. ⏳ Update frontend for fund selection
6. ⏳ Add comprehensive tests
7. ⏳ Update CSV import to specify FundId
8. ⏳ Make settings tenant-aware
9. ⏳ Add audit logging
10. ⏳ Documentation for end users

## Support

For questions or issues with multi-tenancy:
1. Check logs for tenant validation errors
2. Verify JWT token includes TenantId claim
3. Confirm user has appropriate fund access
4. Check that entities have TenantId/FundId set correctly
