# Reset a customer's password

Customers can normally reset their own password from the sign-in page. Use this runbook when the self-service email never arrives.

## Check the email delivery

Search the outbound mail log for the customer's address. If the message shows as bounced, ask the customer for a corrected address and update it on the profile after verifying their identity.

## Send a reset link manually

In the admin console open the customer's profile and choose "Send password reset". The link expires after sixty minutes. Never ask the customer to read a password to you and never set a password on their behalf.

## If the link still does not arrive

Ask the customer to check spam and any corporate mail filters, and to allow mail from our support domain. If the customer uses a shared mailbox, the link may have been opened by an automated scanner, which invalidates it; send a new one.
