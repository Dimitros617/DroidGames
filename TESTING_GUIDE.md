# Testing Guide - Unified Achievement System

## Overview
This guide provides step-by-step instructions to verify that the unified achievement system is working correctly.

## Prerequisites
- Application running successfully
- At least one team registered
- Admin access for testing evaluation achievements

## Test 1: Verify Achievement Display

### Steps:
1. Navigate to `/team/achievements` as a team
2. Verify all achievements are displayed
3. Check that achievements are categorized by rarity

### Expected Results:
- ✅ Total of 23 achievements visible
- ✅ 13 Quiz achievements (IDs starting with "quiz-")
- ✅ 10 Evaluation achievements (other IDs)
- ✅ Rarity filter buttons work correctly
- ✅ Achievement cards show proper icons and descriptions

### Verification:
```
Common: 5 achievements
Rare: 9 achievements  
Epic: 7 achievements
Legendary: 2 achievements
```

## Test 2: Quiz Achievement Unlocking

### Steps:
1. Navigate to `/team/quiz` as a team
2. Answer questions correctly
3. Check for achievement notifications
4. Return to `/team/achievements`
5. Verify unlocked achievements are marked

### Test Achievements:
- **quiz-first-blood**: Answer first question correctly
- **quiz-hat-trick**: Answer 3 questions correctly in a row
- **quiz-speed-demon**: Answer a question in under 5 seconds

### Expected Results:
- ✅ Achievement notification appears when unlocked
- ✅ Achievement shows as unlocked on achievements page
- ✅ Dashboard shows updated achievement count
- ✅ Progress bars update correctly

## Test 3: Evaluation Achievement Unlocking

### Steps:
1. As admin, approve referee scores for a team
2. Ensure the round has appropriate events (crystal touches, etc.)
3. Check that achievements are evaluated
4. Verify team sees unlocked achievements

### Test Achievements:
- **first-crystal-touch**: First team to touch a crystal
- **perfect-run**: Complete round with only crystals, no sulfur
- **speed-demon**: Complete round in less than 10 moves

### Expected Results:
- ✅ Achievements unlocked after score approval
- ✅ Team receives WebSocket notification
- ✅ Achievements visible on team's achievement page
- ✅ Admin page shows updated count

## Test 4: Dashboard Display

### Steps:
1. Navigate to `/team/dashboard` as a team
2. Check achievement count display

### Expected Results:
- ✅ Achievement count matches number of unlocked achievements
- ✅ Count updates when new achievements are unlocked
- ✅ Stats display correctly

## Test 5: Admin Team Edit Page

### Steps:
1. Navigate to `/admin/teams/{teamId}/edit` as admin
2. View team statistics

### Expected Results:
- ✅ Unlocked achievements count displayed correctly
- ✅ Count matches actual unlocked achievements
- ✅ Updates when achievements change

## Test 6: Data Persistence

### Steps:
1. Unlock some achievements
2. Restart the application
3. Navigate to `/team/achievements`

### Expected Results:
- ✅ Unlocked achievements persist after restart
- ✅ Achievement data loaded from TeamAchievement repository
- ✅ No data loss

## Test 7: Achievement Conditions

### Evaluation Achievements to Test:

1. **first-crystal-touch** (Legendary)
   - First team in competition to touch any crystal
   - Should only unlock for one team ever

2. **three-crystals-row** (Rare)
   - Touch 3 crystals consecutively without hitting sulfur
   - Test with appropriate referee scoring

3. **perfect-run** (Epic)
   - Complete round with only crystal touches
   - No sulfur disruptions allowed

4. **crystal-master** (Epic)
   - Touch more than 10 crystals in one round
   - Requires large map or multiple attempts

### Quiz Achievements to Test:

1. **quiz-godlike** (Legendary)
   - Answer all 20 questions correctly without mistakes
   - Hidden achievement

2. **quiz-night-owl** (Common)
   - Answer question between 23:00 and 05:00
   - Test time-based condition

## Troubleshooting

### Issue: Achievements not showing
**Check:**
- TeamAchievement repository has data
- Achievement definitions loaded from achievements.json
- No console errors in browser

### Issue: Achievements not unlocking
**Check:**
- Condition logic in services
- TeamAchievement repository is being written to
- WebSocket notifications working

### Issue: Count mismatch
**Check:**
- All pages using TeamAchievement repository
- No caching issues
- Database consistency

## Success Criteria

✅ All 23 achievements display correctly
✅ Quiz achievements unlock when conditions met
✅ Evaluation achievements unlock after score approval
✅ Counts match across all pages
✅ Data persists across restarts
✅ No console errors
✅ Performance is acceptable

## Performance Notes

- Achievement evaluation happens during score finalization
- Page load times should be < 2 seconds
- Achievement checks should not block UI
- WebSocket notifications should be instant

## Final Verification Checklist

- [ ] All 23 achievements visible on /team/achievements
- [ ] Quiz achievements unlock correctly
- [ ] Evaluation achievements unlock correctly
- [ ] Dashboard shows correct count
- [ ] Admin page shows correct count
- [ ] Rarity filters work
- [ ] Progress bars display
- [ ] Hidden achievements work
- [ ] WebSocket notifications work
- [ ] Data persists after restart
- [ ] No security vulnerabilities
- [ ] No build errors
- [ ] Documentation complete
