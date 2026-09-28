# VPN disconnects at regular intervals

Use this runbook when a customer's VPN connection drops on a predictable schedule, for example every hour.

## Identify the pattern

Ask when the drops happen and whether the interval is constant. A disconnect exactly every sixty minutes usually points to a session or lease timeout rather than a network problem.

## Check the session lifetime

Open the VPN gateway settings and compare the customer's profile with the default. A profile with a maximum session length of one hour will disconnect the client on that interval. Raise the limit or enable automatic re-authentication for the profile.

## Check the client version

Older client versions do not renew their key material and disconnect when it expires. Ask the customer to update to the current client and reconnect.

## Check the network path

If the interval is irregular, ask the customer to test from a different network. Some home routers drop idle UDP mappings; switching the client to TCP mode or enabling keepalive usually resolves it.
