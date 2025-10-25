# Bug Fixes Summary

This document summarizes all bugs that were found and fixed in the codebase.

## Critical Bugs Fixed

### 1. Vote Update Logic Error (VoteController.cs:95)
**Issue**: When updating a vote from downvote to upvote, the logic was backwards - it incremented DownVotes and decremented UpVotes instead of the reverse.

**Fix**: Corrected the logic to properly decrement DownVotes and increment UpVotes when changing from downvote to upvote.

### 2. Top 5 Resources Sorted Wrong (ResourceController.cs:44)
**Issue**: Used `OrderBy(x => x.Score)` which returns the LOWEST scoring resources instead of highest.

**Fix**: Changed to `OrderByDescending(x => x.Score)` to return the top-ranked resources.

### 3. Integer Underflow Risk (VoteController.cs)
**Issue**: Vote counts use `ulong` which wraps to UInt64.MaxValue if decremented below zero.

**Fix**: Added checks to ensure counts are greater than 0 before decrementing.

### 4. Missing Authorization Headers (apiHelper.js)
**Issue**: The `authorizedFetch` helper method never actually added the Authorization header, so every call manually added it.

**Fix**: Modified `authorizedFetch` to automatically add the Authorization header from the user store.

### 5. Race Condition in Voting (VoteController.cs:39-47)
**Issue**: Check for existing vote and creation were not atomic - concurrent requests could create duplicate votes.

**Fix**: Wrapped vote creation in a database transaction to ensure atomicity.

## Security & Data Integrity Fixes

### 6. No Login Response Validation (user.js:14-27)
**Issue**: If login failed, invalid data was still saved to localStorage.

**Fix**: Added response validation to check for `response.ok` and required fields before saving. Added error cleanup to remove localStorage items on failure.

### 7. Sensitive Data Exposure (CategoryController.cs:20)
**Note**: Returns full Category entities with navigation properties. The comment on line 12 acknowledges this needs DTOs. This is a known issue but not fixed as it requires architectural changes.

### 8. Missing EF Core Includes (Multiple controllers)
**Issue**: Navigation properties accessed without `.Include()` causing null reference exceptions or N+1 query issues.

**Fix**: Added `.Include()` for all navigation properties in:
- ResourceController: Include Owner and Section
- CategoryController: Include Owner and Sections
- SectionController: Include Owner, Category, and Resources
- VoteController: Include Resource

### 9. Resources/Sections Could Belong to Multiple Parents
**Issue**: No validation prevented resources from being added to multiple sections or sections to multiple categories.

**Fix**: Added checks in:
- `CategoryController.AddAsync`: Verify section doesn't already have a category
- `SectionController.AddAsync`: Verify resource doesn't already have a section

### 10. Category Update Ignored Description (CategoryController.cs:112-113)
**Issue**: The update method only updated Name, not Description.

**Fix**: Added `category.Description = request.Description;`

## Algorithm Fixes

### 11. Incorrect Epoch Calculation (VotingAlgorithm.cs:12)
**Issue**: Used `td.Seconds` (0-59 component) instead of `td.TotalSeconds`, producing incorrect hot ranking scores.

**Fix**: Changed to `return td.TotalSeconds;`

## Frontend Fixes

### 12. Incorrect API URL (Categories.vue:6)
**Issue**: Used relative URL without base URL, causing fetch to fail.

**Fix**: Changed to use `ApiHelper.authorizedFetch` with proper base URL.

### 13. Missing Table Body Elements (Profile.vue:104, 117)
**Issue**: Sections and Resources tables missing `<tbody>`, so data wouldn't render.

**Fix**: Added `<tbody>` elements with proper v-for loops to render sections and resources.

### 14. Confusing Function Naming (CreateCategory.vue:12-14, 44)
**Issue**: Local function named `onMounted` conflicted with Vue's lifecycle hook import.

**Fix**: Renamed local function to `checkAuth` and `onButtonClick` to `createCategory` for clarity.

### 15. Redundant Authorization Headers (Profile.vue, CreateCategory.vue)
**Issue**: Manually adding Authorization headers in every API call since the helper didn't do it.

**Fix**: Removed redundant header specifications now that `ApiHelper.authorizedFetch` handles it automatically.

## Additional Improvements

### 16. Better Error Handling
- Added try-catch blocks with proper error messages in Vue components
- Changed from `alert()` to `console.error()` for better error logging
- Added error cleanup in login flow

### 17. Optimized Duplicate Checks
- ResourceController: Changed from loading all resources into memory to using `AnyAsync()` query for duplicate detection

## Remaining Known Issues (Not Fixed)

These issues were identified but not fixed as they require more substantial changes:

1. **Hardcoded JWT Secret** (appsettings.json:8) - Should use environment variables
2. **Tokens in localStorage** (user.js) - Should use HttpOnly cookies for better security
3. **No Token Expiration Checking** (user.js:51-62) - Only checks if token exists, not if valid
4. **Empty Paging Class** (Paging/Page.cs) - Unused placeholder class
5. **Non-functional Edit/Delete Buttons** (Profile.vue) - Buttons exist but have no handlers
6. **Inconsistent User Fetching** (UserController.cs:13) - Uses extension method instead of base controller method

## Testing Recommendations

After these fixes, you should test:

1. Vote creation, update, and deletion flows
2. Category/Section/Resource CRUD operations
3. Login/logout flows with error cases
4. Profile page rendering with user data
5. Top 5 resources query returns correct sorted results
6. Attempting to add resources/sections to multiple parents is properly rejected

## Files Modified

### Backend (C#)
- `iteration1/Controllers/VoteController.cs`
- `iteration1/Controllers/ResourceController.cs`
- `iteration1/Controllers/CategoryController.cs`
- `iteration1/Controllers/SectionController.cs`
- `iteration1/Voting/VotingAlgorithm.cs`

### Frontend (Vue/JS)
- `thebestclient/src/api/apiHelper.js`
- `thebestclient/src/stores/user.js`
- `thebestclient/src/views/Categories.vue`
- `thebestclient/src/views/Profile.vue`
- `thebestclient/src/views/editing/CreateCategory.vue`
