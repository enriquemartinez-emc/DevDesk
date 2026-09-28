# Unlock a locked-out account

Accounts lock automatically after three failed sign-in or 2FA attempts within fifteen minutes.

## Confirm the lockout

In the admin console, open the customer's profile and check the "Status" field. A locked account shows "Locked (too many attempts)" along with the time the lock will expire.

## Wait or unlock

Locks expire on their own after thirty minutes. If the customer cannot wait, choose "Unlock account" on the profile. Only unlock after verifying the customer's identity using the same checks as a 2FA reset.

## If the lockout keeps recurring

Repeated lockouts after failed 2FA attempts usually mean the customer's authenticator clock is out of sync or they enrolled on a device they no longer have. Follow the "Reset a customer's 2FA" runbook to re-enroll them.

## Watch for abuse

Many lockouts across different accounts from the same IP address may indicate a credential-stuffing attempt. Escalate to the security team instead of unlocking accounts.
