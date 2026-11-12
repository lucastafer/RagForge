# Orbitly Incident Response Runbook

This runbook belongs to the fictional company Orbitly and is used as demo data for RagForge. Names, tools, and timings are invented.

## Severity levels

- **SEV1:** complete outage or data loss affecting all customers. Must be acknowledged within 5 minutes, and a status page update must be published within 15 minutes.
- **SEV2:** a major feature is unavailable or severely degraded for many customers, for example sync failures or login errors. Must be acknowledged within 15 minutes.
- **SEV3:** a minor feature is broken or a small group of customers is affected and a workaround exists. Must be acknowledged within 4 business hours.

When in doubt, declare the higher severity. Downgrading an incident later is cheap; reacting slowly to a real outage is not.

## On-call rotation

Each engineering squad has a primary and a secondary on-call engineer. Rotations last one week and change every Monday at 10:00 UTC. Pages are sent through PagerDuty. If the primary does not acknowledge a page within 10 minutes, it escalates to the secondary, and after another 10 minutes to the engineering manager.

On-call engineers receive a fixed compensation of 300 USD per week, plus one day off after any week in which they handled a SEV1 incident outside working hours.

## Declaring an incident

1. Run `/incident declare` in the #incidents Slack channel and select the severity.
2. The bot creates a dedicated channel named `inc-<date>-<short-name>` and a video call.
3. The person who declared the incident becomes the Incident Commander until they explicitly hand the role over.
4. The Incident Commander assigns a Communications Lead for every SEV1 and SEV2 incident.

## Roles

The **Incident Commander** coordinates the response, decides on mitigations, and keeps a timeline. They should not debug the problem themselves.

The **Communications Lead** updates the public status page every 30 minutes for SEV1 and every 60 minutes for SEV2, and keeps Customer Support informed.

**Subject matter experts** investigate and apply fixes. Rollbacks are always preferred over hotfixes during an active incident.

## After the incident

A blameless postmortem is mandatory for every SEV1 and SEV2 incident. The postmortem document must be published within 5 business days and must include a timeline, the root cause, the customer impact, and action items with owners and due dates. Postmortems are reviewed in the weekly reliability meeting every Thursday.
