# Reset a customer's 2FA

Use this runbook when a customer has lost their authenticator device or cannot pass two-factor authentication.

## Verify the customer's identity

Before changing anything, confirm the requester owns the account. Ask for the account email, the last four digits of the payment method on file, and the date of the most recent invoice. Do not proceed if any answer does not match.

## Reset the second factor

Open the admin console, search for the customer by email, and choose "Security". Select "Reset two-factor authentication" and confirm. The customer's existing authenticator enrollment is removed and all active sessions are signed out.

## Re-enroll the customer

Ask the customer to sign in with their password. They will be prompted to enroll a new authenticator app. Recovery codes are regenerated at this point; remind the customer to store them somewhere safe.

## Record the change

Add a note to the ticket stating which identity checks passed and that the second factor was reset. Resolve the ticket once the customer confirms they can sign in.
