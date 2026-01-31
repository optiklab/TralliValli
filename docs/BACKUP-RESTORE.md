# Backup and Restore Guide

This guide covers backup and restore operations for TralliValli, including automatic backups, manual triggers, restore procedures, point-in-time recovery, and disaster recovery planning.

## Table of Contents

- [Overview](#overview)
- [Automatic Backup Schedule](#automatic-backup-schedule)
- [Backup File Locations](#backup-file-locations)
- [Manual Backup Trigger](#manual-backup-trigger)
- [Restore Procedures](#restore-procedures)
- [Point-in-Time Recovery (PITR)](#point-in-time-recovery-pitr)
- [Disaster Recovery Plan](#disaster-recovery-plan)
- [Backup Verification](#backup-verification)
- [Troubleshooting](#troubleshooting)

---

## Overview

TralliValli uses a comprehensive backup strategy to protect your data:

- **Automated Daily Backups**: MongoDB collections are automatically backed up to Azure Blob Storage
- **Retention Policy**: Backups are retained for 30 days by default (configurable)
- **Manual Triggers**: Administrators can trigger backups on-demand via API
- **Multiple Restore Options**: API-based or manual restore using MongoDB tools
- **Collections Backed Up**: users, conversations, messages, invites, files

**Backup Architecture:**
- **Storage**: Azure Blob Storage (encrypted at rest)
- **Format**: BSON compressed with gzip (60-80% compression ratio)
- **Scheduler**: Cron-based background worker
- **Protection**: Circuit breaker pattern for Azure Storage failures

---

## Automatic Backup Schedule

### Default Schedule

Backups run automatically **daily at 3:00 AM UTC** by default. The schedule is configured using cron expressions.

**Configuration Location:**
- `appsettings.json`: `BackupWorker:CronSchedule`
- Environment variable: `BACKUP_CRON_SCHEDULE`

### Schedule Examples

| Schedule | Description |
|----------|-------------|
| `0 3 * * *` | Daily at 3:00 AM UTC (default) |
| `0 0 * * *` | Daily at midnight UTC |
| `0 2 * * 0` | Weekly on Sunday at 2:00 AM UTC |
| `0 4 1 * *` | Monthly on the 1st at 4:00 AM UTC |
| `0 */6 * * *` | Every 6 hours |

### How Automatic Backups Work

1. **Scheduler**: The BackupWorker runs as a hosted service, waiting for the next scheduled time
2. **Export**: Each collection is exported to BSON format in memory
3. **Compress**: BSON data is compressed using gzip (typically 60-80% reduction)
4. **Upload**: Compressed backups are uploaded to Azure Blob Storage
5. **Cleanup**: Backups older than the retention period are automatically deleted
6. **Logging**: Success/failure metrics and timing are recorded

### Monitoring Automatic Backups

Check application logs for backup status:

```bash
# View recent backup logs
docker logs tralivali-api | grep "BackupWorker"

# Or in Azure Container Apps
az containerapp logs show --name tralivali-api --resource-group tralivali-rg --follow
```

**Log Examples:**
```
BackupWorker starting with cron schedule: 0 3 * * *
Next backup run scheduled for 2024-01-16 03:00:00 UTC (8h 45m from now)
Starting backup process at 2024-01-16 03:00:00
Backing up collection: users
Exported 1523 documents from users, BSON size: 524288 bytes
Compressed users: 104857 bytes (compression: 80.0%)
Uploaded backup to: backups/2024-01-16/tralivali_users.bson.gz
Backup process completed in 12.34 seconds. Success: 5, Failed: 0
Cleaned up 2 old backup(s)
```

### Backup Retention

**Default Retention:** 30 days

Backups older than the retention period are automatically deleted during each backup run.

**Configuration:**
```json
{
  "BackupWorker": {
    "RetentionDays": 30
  }
}
```

Or via environment variable:
```bash
BACKUP_RETENTION_DAYS=30
```

**Note:** You can also configure lifecycle management policies in Azure Storage Account for additional cleanup layers.

---

## Backup File Locations

### Azure Blob Storage Structure

Backups are stored in Azure Blob Storage with the following structure:

```
{container-name}/
└── backups/
    ├── 2024-01-15/
    │   ├── tralivali_users.bson.gz
    │   ├── tralivali_conversations.bson.gz
    │   ├── tralivali_messages.bson.gz
    │   ├── tralivali_invites.bson.gz
    │   └── tralivali_files.bson.gz
    ├── 2024-01-16/
    │   ├── tralivali_users.bson.gz
    │   ├── tralivali_conversations.bson.gz
    │   ├── tralivali_messages.bson.gz
    │   ├── tralivali_invites.bson.gz
    │   └── tralivali_files.bson.gz
    └── ...
```

### Path Format

**Pattern:** `backups/{YYYY-MM-DD}/tralivali_{collection}.bson.gz`

**Examples:**
- `backups/2024-01-15/tralivali_users.bson.gz`
- `backups/2024-01-15/tralivali_messages.bson.gz`

This structure provides:
- **Organization**: Easy to identify backups by date
- **Restoration**: Simple to restore entire day's backup
- **Cleanup**: Efficient deletion of old backups
- **Identification**: Clear collection naming

### Configuration

**Blob Storage Connection String:**
```json
{
  "BackupWorker": {
    "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "BlobContainerName": "backups"
  }
}
```

**Environment Variables:**
```bash
BACKUP_BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=..."
BACKUP_BLOB_CONTAINER_NAME="backups"
```

### Accessing Backup Files

#### Using Azure Portal

1. Navigate to your Storage Account in [Azure Portal](https://portal.azure.com)
2. Select "Containers" under "Data storage"
3. Click on your backup container (default: `backups`)
4. Browse the `backups/` folder by date

#### Using Azure CLI

```bash
# List all backup dates
az storage blob list \
  --account-name <storage-account-name> \
  --container-name backups \
  --prefix "backups/" \
  --query "[].name" \
  --output table

# List backups for a specific date
az storage blob list \
  --account-name <storage-account-name> \
  --container-name backups \
  --prefix "backups/2024-01-15/" \
  --output table
```

#### Using Azure Storage Explorer

1. Download [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)
2. Connect to your storage account
3. Navigate to Blob Containers → backups → backups
4. Browse folders by date

---

## Manual Backup Trigger

Administrators can trigger backups on-demand using the admin API.

### API Endpoint

**Endpoint:** `POST /admin/backup/trigger`

**Authentication:** Requires admin role JWT token

**Request:**
```bash
curl -X POST https://yourdomain.com/admin/backup/trigger \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Response (Success):**
```json
{
  "backupId": "507f1f77bcf86cd799439011",
  "message": "Backup completed successfully",
  "status": "Completed"
}
```

**Response (Failure):**
```json
{
  "backupId": "507f1f77bcf86cd799439011",
  "message": "Backup failed: Connection to Azure Blob Storage failed",
  "status": "Failed"
}
```

### When to Trigger Manual Backups

Consider triggering manual backups:

- **Before major upgrades**: Backup before deploying new versions
- **Before bulk operations**: Backup before mass data changes
- **After critical changes**: Backup after important configuration changes
- **On-demand recovery points**: Create specific recovery points
- **Testing**: Verify backup process is working

### Listing Available Backups

**Endpoint:** `GET /admin/backup/list`

**Request:**
```bash
curl -X GET https://yourdomain.com/admin/backup/list \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

**Response:**
```json
{
  "backups": [
    {
      "id": "507f1f77bcf86cd799439011",
      "createdAt": "2024-01-16T03:00:00Z",
      "filePath": "backups/2024-01-16",
      "size": 10485760,
      "type": "scheduled",
      "status": "Completed"
    },
    {
      "id": "507f1f77bcf86cd799439012",
      "createdAt": "2024-01-15T14:30:00Z",
      "filePath": "backups/2024-01-15",
      "size": 10240000,
      "type": "manual",
      "status": "Completed"
    }
  ]
}
```

### Using the Web Interface (Future)

A web-based admin dashboard for backup management is planned for future releases, which will provide:
- Visual backup history
- One-click backup triggers
- Restore point selection
- Backup status monitoring

---

## Restore Procedures

There are two methods to restore backups: API-based restore and manual restore using MongoDB tools.

### Method 1: API-Based Restore (Recommended)

The easiest way to restore is using the admin API, which handles all steps automatically.

**Endpoint:** `POST /admin/backup/restore/{date}`

**Request:**
```bash
curl -X POST https://yourdomain.com/admin/backup/restore/2024-01-15 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Response (Success):**
```json
{
  "message": "Backup restored successfully",
  "success": true
}
```

**Response (Failure):**
```json
{
  "message": "Backup restore failed",
  "success": false
}
```

**⚠️ WARNING:**
- This operation will **DELETE ALL existing data** in the database
- All collections will be replaced with data from the backup
- Users will be disconnected and need to re-authenticate
- **Always verify the backup date before restoring**

**Process:**
1. Downloads backup files from Azure Blob Storage for the specified date
2. Decompresses each collection backup
3. Clears existing collection data
4. Restores documents from backup
5. Logs restoration progress and completion

### Method 2: Manual Restore Using MongoDB Tools

For more control or troubleshooting, you can manually restore backups using MongoDB tools.

#### Step 1: Download Backup Files

Download backups from Azure Blob Storage:

```bash
# Download a single collection
az storage blob download \
  --account-name <storage-account-name> \
  --container-name backups \
  --name backups/2024-01-15/tralivali_users.bson.gz \
  --file users.bson.gz \
  --auth-mode key

# Download all collections for a date
az storage blob download-batch \
  --account-name <storage-account-name> \
  --source backups \
  --destination ./backup-2024-01-15 \
  --pattern "backups/2024-01-15/*.bson.gz" \
  --auth-mode key
```

**Alternative using Azure Storage Explorer:**
1. Navigate to the backup date folder
2. Select all `.bson.gz` files
3. Click "Download"
4. Save to local directory

#### Step 2: Decompress Backup Files

```bash
# Decompress a single file
gunzip users.bson.gz

# Decompress all files in directory
gunzip ./backup-2024-01-15/*.bson.gz
```

**Result:** You'll have `.bson` files ready for restoration.

#### Step 3: Restore to MongoDB

**Restore Single Collection:**
```bash
mongorestore \
  --uri="mongodb://username:password@mongodb-host:27017/tralivali" \
  --collection=users \
  --drop \
  users.bson
```

**Restore All Collections:**
```bash
# Users
mongorestore --uri="mongodb://..." --collection=users --drop backup-2024-01-15/tralivali_users.bson

# Conversations
mongorestore --uri="mongodb://..." --collection=conversations --drop backup-2024-01-15/tralivali_conversations.bson

# Messages
mongorestore --uri="mongodb://..." --collection=messages --drop backup-2024-01-15/tralivali_messages.bson

# Invites
mongorestore --uri="mongodb://..." --collection=invites --drop backup-2024-01-15/tralivali_invites.bson

# Files
mongorestore --uri="mongodb://..." --collection=files --drop backup-2024-01-15/tralivali_files.bson
```

**mongorestore Options:**
- `--uri`: MongoDB connection string (include database name)
- `--collection`: Target collection name
- `--drop`: Drop collection before restoring (recommended)
- `--nsInclude`: Filter specific namespaces (advanced)

#### Step 4: Verify Restoration

```bash
# Connect to MongoDB
mongosh "mongodb://username:password@mongodb-host:27017/tralivali"

# Check document counts
db.users.countDocuments()
db.conversations.countDocuments()
db.messages.countDocuments()
db.invites.countDocuments()
db.files.countDocuments()

# Sample some documents to verify data
db.users.findOne()
db.messages.find().limit(5)
```

### Partial Restore

To restore only specific collections (e.g., only users):

**API Method:**
Not supported via API. Use manual method below.

**Manual Method:**
```bash
# Download and decompress only the users collection
az storage blob download \
  --account-name <storage-account-name> \
  --container-name backups \
  --name backups/2024-01-15/tralivali_users.bson.gz \
  --file users.bson.gz

gunzip users.bson.gz

# Restore only users collection
mongorestore --uri="mongodb://..." --collection=users --drop users.bson
```

### Pre-Restore Checklist

Before restoring, ensure:

- [ ] You have identified the correct backup date
- [ ] You have verified the backup files exist and are not corrupted
- [ ] You have taken a current backup as a safety measure (if data is not corrupt)
- [ ] You have notified users of potential downtime
- [ ] You have admin access to the database
- [ ] You understand that existing data will be replaced

---

## Point-in-Time Recovery (PITR)

Point-in-Time Recovery allows you to restore data to a specific moment in time.

### Available Recovery Points

Recovery points are available based on your backup schedule:

- **Daily Backups (default)**: Recovery points every 24 hours at 3:00 AM UTC
- **Custom Schedule**: Recovery points based on your cron schedule
- **Manual Backups**: Additional recovery points when manually triggered

**Example Recovery Timeline:**
```
Jan 13 03:00 UTC → Automatic backup
Jan 14 03:00 UTC → Automatic backup
Jan 14 15:30 UTC → Manual backup (before upgrade)
Jan 15 03:00 UTC → Automatic backup
Jan 15 16:00 UTC → Current time (data loss occurred)
```

You can restore to any of these points.

### PITR Procedure

#### 1. Identify Recovery Point

Determine the last known good state of your data:

```bash
# List available backups with dates
curl -X GET https://yourdomain.com/admin/backup/list \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

Or using Azure CLI:
```bash
az storage blob list \
  --account-name <storage-account-name> \
  --container-name backups \
  --prefix "backups/" \
  --query "[].name" \
  --output table
```

#### 2. Restore to Selected Point

**Using API:**
```bash
# Restore to January 14, 2024 at 03:00 UTC
curl -X POST https://yourdomain.com/admin/backup/restore/2024-01-14 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

**Using Manual Method:**
Follow the [Manual Restore](#method-2-manual-restore-using-mongodb-tools) procedure with the selected date.

#### 3. Verify Recovery

After restoration:

1. **Test Core Functionality**: Login, send messages, verify conversations
2. **Check Data Integrity**: Verify user counts, message counts
3. **Review Recent Data**: Check for expected recent entries
4. **User Validation**: Have key users verify their data

### PITR Limitations

- **Granularity**: Limited to backup frequency (daily by default)
  - Cannot recover to points between backups
  - Data created after last backup will be lost
  
- **Data Loss Window**: 
  - Daily backups: Up to 24 hours of data loss
  - Hourly backups: Up to 1 hour of data loss
  - Recommendation: Increase backup frequency for critical systems

- **No Transaction Logs**: MongoDB BSON backups don't include transaction logs
  - Cannot replay operations between backups
  - Consider MongoDB Atlas with continuous backups for zero RPO

### Improving PITR Granularity

To reduce potential data loss:

**Option 1: Increase Backup Frequency**
```json
{
  "BackupWorker": {
    "CronSchedule": "0 */6 * * *"  // Every 6 hours
  }
}
```

**Option 2: Trigger Manual Backups Before Critical Operations**
```bash
# Before deployment
curl -X POST https://yourdomain.com/admin/backup/trigger ...

# Before bulk data operations
curl -X POST https://yourdomain.com/admin/backup/trigger ...
```

**Option 3: MongoDB Atlas (Future Consideration)**
- Continuous backups with oplog
- Point-in-time recovery to any second
- Zero RPO (Recovery Point Objective)

---

## Disaster Recovery Plan

This section outlines procedures for recovering from catastrophic failures.

### Disaster Scenarios

1. **Complete Data Center Failure**: Azure region outage
2. **Database Corruption**: MongoDB data corruption
3. **Accidental Data Deletion**: Mass deletion of data
4. **Security Breach**: Compromised database
5. **Storage Account Deletion**: Accidental deletion of Azure Storage

### Recovery Strategy

#### RTO and RPO

**Recovery Time Objective (RTO):** Time to restore service
- **Target RTO**: 2-4 hours (depending on data size)
- **Actual RTO**: Varies with backup size and network speed

**Recovery Point Objective (RPO):** Maximum acceptable data loss
- **Current RPO**: 24 hours (daily backups)
- **Improved RPO**: Configure hourly backups for critical systems

#### Disaster Recovery Steps

### Scenario 1: Complete Data Loss (Database Destroyed)

**Symptoms:**
- MongoDB instance unresponsive or destroyed
- All collections empty or corrupted
- Cannot connect to database

**Recovery Steps:**

1. **Create New MongoDB Instance** (15-30 minutes)
   ```bash
   # Using Docker
   docker run -d --name mongodb \
     -p 27017:27017 \
     -e MONGO_INITDB_ROOT_USERNAME=admin \
     -e MONGO_INITDB_ROOT_PASSWORD=<strong-password> \
     mongo:7
   
   # Or provision Azure Cosmos DB for MongoDB
   ```

2. **Update Application Configuration** (5 minutes)
   - Update MongoDB connection string in `appsettings.json` or environment variables
   - Restart application services

3. **Restore Latest Backup** (30-120 minutes)
   ```bash
   # Using API (recommended)
   curl -X POST https://yourdomain.com/admin/backup/restore/2024-01-15 \
     -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
   
   # Or manual restore (see Manual Restore section)
   ```

4. **Verify Data Integrity** (15-30 minutes)
   - Check document counts in all collections
   - Test authentication and authorization
   - Verify message delivery
   - Test file uploads/downloads

5. **Notify Users** (5 minutes)
   - Inform users of restoration
   - Explain potential data loss window
   - Request validation from key users

**Estimated Total Recovery Time:** 2-4 hours

### Scenario 2: Azure Region Outage

**Symptoms:**
- Cannot access Azure Blob Storage
- Cannot reach MongoDB instance
- Application unresponsive

**Recovery Steps:**

1. **Verify Outage** (5 minutes)
   - Check [Azure Status](https://status.azure.com/)
   - Verify affected services and regions

2. **Option A: Wait for Recovery**
   - Azure SLA: 99.9% uptime (8.76 hours downtime/year max)
   - Most outages resolve within 1-2 hours
   - No action required if acceptable

3. **Option B: Failover to Different Region** (3-6 hours)
   - Deploy infrastructure in alternate Azure region
   - Restore latest backup from geo-redundant storage
   - Update DNS to point to new deployment
   - **Note:** Requires geo-redundant storage configuration

### Scenario 3: Backup Storage Deleted

**Symptoms:**
- Backup container deleted from Azure Storage
- Cannot list or access backups

**Recovery Steps:**

1. **Check Soft Delete** (5 minutes)
   ```bash
   # List deleted containers
   az storage container list \
     --account-name <storage-account-name> \
     --include-deleted
   
   # Restore deleted container
   az storage container restore \
     --account-name <storage-account-name> \
     --name backups \
     --deleted-version <version-id>
   ```

2. **If Soft Delete Not Enabled:**
   - **Data Loss**: All backups are unrecoverable
   - **Immediate Action**: Trigger manual backup immediately
   - **Recovery**: Cannot restore, must continue with current data
   - **Prevention**: Enable soft delete and immutability policies

### Scenario 4: Ransomware/Security Breach

**Symptoms:**
- Unauthorized database access
- Data encrypted or modified
- Suspicious backup deletions

**Recovery Steps:**

1. **Isolate Systems** (Immediate)
   - Disconnect application from internet
   - Revoke all access tokens and passwords
   - Disable user accounts temporarily

2. **Assess Damage** (30-60 minutes)
   - Review audit logs
   - Identify compromised data
   - Determine earliest clean backup

3. **Restore Clean Backup** (1-3 hours)
   - Use backup before compromise occurred
   - Restore to isolated environment first
   - Verify data integrity

4. **Secure Systems** (2-4 hours)
   - Reset all passwords (database, admin, users)
   - Rotate API keys and secrets
   - Enable MFA for admin accounts
   - Update security rules and firewall

5. **Resume Operations** (1-2 hours)
   - Reconnect application
   - Force all users to reset passwords
   - Monitor for suspicious activity

**Estimated Total Recovery Time:** 6-12 hours

### Disaster Prevention

#### Geo-Redundant Backups

Configure Azure Storage for geo-redundancy:

```bash
# Enable geo-redundant storage (GRS)
az storage account update \
  --name <storage-account-name> \
  --resource-group <resource-group> \
  --sku Standard_GRS

# Or zone-redundant storage (ZRS) for single region redundancy
az storage account update \
  --name <storage-account-name> \
  --resource-group <resource-group> \
  --sku Standard_ZRS
```

#### Immutable Storage

Protect backups from deletion:

```bash
# Enable immutable storage with time-based retention
az storage container immutability-policy create \
  --account-name <storage-account-name> \
  --container-name backups \
  --period 30
```

#### Soft Delete

Enable soft delete for accidental deletion protection:

```bash
# Enable blob soft delete (7 days retention)
az storage account blob-service-properties update \
  --account-name <storage-account-name> \
  --enable-delete-retention true \
  --delete-retention-days 7
```

#### Backup Testing

Regularly test disaster recovery:

1. **Monthly**: Verify backups are running and files exist
2. **Quarterly**: Perform test restore to staging environment
3. **Annually**: Execute full disaster recovery simulation

#### Emergency Contact List

Maintain a contact list for disaster scenarios:

| Role | Name | Contact | Responsibility |
|------|------|---------|----------------|
| Primary Admin | [Name] | [Email/Phone] | Initial response, coordination |
| Database Admin | [Name] | [Email/Phone] | MongoDB restoration |
| Azure Admin | [Name] | [Email/Phone] | Azure resource management |
| Security Lead | [Name] | [Email/Phone] | Security breach response |
| Communication Lead | [Name] | [Email/Phone] | User communication |

---

## Backup Verification

Regular verification ensures backups are valid and restorable.

### Automated Verification

The BackupWorker logs backup metrics after each run:

```
Backup process completed in 12.34 seconds. Success: 5, Failed: 0
```

Monitor logs for:
- All 5 collections backed up successfully
- Reasonable backup sizes (not 0 bytes)
- No circuit breaker failures
- Cleanup completing successfully

### Manual Verification

#### Monthly Verification Checklist

1. **Check Backup Existence** (5 minutes)
   ```bash
   # List recent backups
   az storage blob list \
     --account-name <storage-account-name> \
     --container-name backups \
     --prefix "backups/" \
     --query "[-30:].name" \
     --output table
   ```
   
   Verify:
   - [ ] Backups exist for each day
   - [ ] All 5 collections present per date
   - [ ] File sizes are reasonable (not 0 bytes)

2. **Check Application Logs** (5 minutes)
   ```bash
   docker logs tralivali-api | grep -i backup | tail -50
   ```
   
   Verify:
   - [ ] No backup failures in recent runs
   - [ ] Cleanup operations completing
   - [ ] Circuit breaker not in open state

3. **Verify Storage Health** (5 minutes)
   - Check Azure Storage Account metrics in Azure Portal
   - Verify storage capacity not exceeded
   - Check for throttling or errors

#### Quarterly Test Restore

Perform a test restore to verify backup integrity:

1. **Create Test Environment** (30 minutes)
   - Spin up test MongoDB instance
   - Configure test application instance
   - Ensure isolated from production

2. **Restore Latest Backup** (30-60 minutes)
   - Download latest backup files
   - Restore to test MongoDB
   - Verify all collections restored

3. **Validate Data** (30 minutes)
   - Check document counts match expectations
   - Test application functionality
   - Verify data relationships (messages → conversations → users)
   - Test file metadata references

4. **Document Results** (15 minutes)
   - Record restoration time
   - Note any issues encountered
   - Update disaster recovery procedures if needed

### Backup Health Metrics

Monitor these metrics:

| Metric | Healthy Range | Action If Outside |
|--------|---------------|-------------------|
| Backup Success Rate | 100% | Investigate failures immediately |
| Backup Duration | 10-60 seconds | Check database/network performance |
| Compressed Size | 1-10 GB | Verify compression working, check growth |
| Storage Used | < 1 TB | Consider cleanup or tier changes |
| Retention Gap | 0 days | Verify scheduler running |

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: Backups Not Running

**Symptoms:**
- No recent backups in storage
- No backup logs in application

**Diagnosis:**
```bash
# Check if BackupWorker is running
docker logs tralivali-api | grep "BackupWorker"

# Should see: "BackupWorker starting with cron schedule: ..."
```

**Solutions:**

1. **Check Configuration**
   ```bash
   # Verify environment variables
   echo $BACKUP_CRON_SCHEDULE
   echo $BACKUP_BLOB_STORAGE_CONNECTION_STRING
   echo $BACKUP_BLOB_CONTAINER_NAME
   ```
   
   Ensure:
   - Connection string is valid
   - Cron schedule is valid
   - Container name is correct

2. **Verify Azure Storage Access**
   ```bash
   # Test connection
   az storage container show \
     --name backups \
     --account-name <storage-account-name>
   ```

3. **Check Application Startup**
   - Ensure BackupWorker is registered in `Program.cs`
   - Verify no startup exceptions
   - Check all dependencies injected correctly

#### Issue: Backup Fails with Circuit Breaker Error

**Symptoms:**
- Logs show "Circuit breaker is open"
- Backups failing repeatedly

**Diagnosis:**
```bash
docker logs tralivali-api | grep -i "circuit breaker"
```

**Solutions:**

1. **Check Azure Storage Health**
   - Verify storage account not throttled
   - Check for Azure outages: https://status.azure.com
   - Review storage account metrics

2. **Adjust Circuit Breaker Settings**
   ```json
   {
     "BackupWorker": {
       "CircuitBreakerFailureThreshold": 5,
       "CircuitBreakerTimeoutSeconds": 30
     }
   }
   ```
   
   - Increase timeout if network is slow
   - Increase threshold if transient errors common

3. **Wait for Circuit Breaker Reset**
   - Circuit breaker resets after timeout period
   - Next backup run will retry automatically

#### Issue: Restore Fails

**Symptoms:**
- API returns "Backup restore failed"
- Partial data restored

**Diagnosis:**
```bash
# Check restore logs
docker logs tralivali-api | grep -i restore

# Check MongoDB logs
docker logs mongodb
```

**Solutions:**

1. **Verify Backup Files Exist**
   ```bash
   az storage blob list \
     --account-name <storage-account-name> \
     --container-name backups \
     --prefix "backups/2024-01-15/"
   ```
   
   Ensure all 5 collection files present.

2. **Check MongoDB Space**
   ```bash
   mongosh "mongodb://..." --eval "db.stats()"
   ```
   
   Ensure sufficient disk space for restore.

3. **Use Manual Restore**
   - Download backup files manually
   - Decompress locally
   - Restore using mongorestore with verbose logging
   - Identify specific failure point

4. **Check MongoDB Permissions**
   - Ensure user has write permissions
   - Verify database name correct
   - Check collection-level permissions

#### Issue: Backup Files Corrupted

**Symptoms:**
- Cannot decompress backup files
- Restore fails with deserialization errors

**Diagnosis:**
```bash
# Test decompression
gunzip -t backup-file.bson.gz

# If successful, test BSON validity
# (No direct tool, but mongorestore will validate)
```

**Solutions:**

1. **Use Previous Backup**
   - Try restoring from day before
   - Check multiple recent backups

2. **Verify Download Integrity**
   - Re-download from Azure Storage
   - Use Azure Storage Explorer for verification

3. **Check Storage Account Health**
   - Review Azure Storage metrics
   - Check for bit rot or storage issues
   - Consider enabling redundancy

#### Issue: Backup Taking Too Long

**Symptoms:**
- Backups exceeding 30 minutes
- Application performance degraded during backup

**Diagnosis:**
```bash
# Check backup duration in logs
docker logs tralivali-api | grep "Backup process completed in"

# Check database size
mongosh "mongodb://..." --eval "db.stats().dataSize"
```

**Solutions:**

1. **Optimize Database**
   - Add indexes for common queries
   - Archive old messages
   - Consider sharding for very large datasets

2. **Increase Resources**
   - Allocate more CPU/memory to application
   - Use faster Azure storage tier
   - Increase MongoDB resources

3. **Adjust Backup Schedule**
   - Move to off-peak hours
   - Reduce backup frequency if acceptable

4. **Consider Incremental Backups**
   - Future enhancement to backup only changes
   - Currently not supported

#### Issue: Insufficient Storage Space

**Symptoms:**
- Backups failing with storage errors
- Azure Storage quota exceeded

**Diagnosis:**
```bash
# Check storage usage
az storage account show-usage \
  --name <storage-account-name> \
  --resource-group <resource-group>
```

**Solutions:**

1. **Reduce Retention Period**
   ```json
   {
     "BackupWorker": {
       "RetentionDays": 14  // Reduced from 30
     }
   }
   ```

2. **Move Old Backups to Archive Tier**
   ```bash
   # Change storage tier for old backups
   az storage blob set-tier \
     --account-name <storage-account-name> \
     --container-name backups \
     --name "backups/2024-01-01/*" \
     --tier Archive
   ```

3. **Manually Delete Old Backups**
   ```bash
   # List backups older than 60 days
   az storage blob list \
     --account-name <storage-account-name> \
     --container-name backups \
     --prefix "backups/" \
     --query "[?properties.lastModified < '2023-12-01'].name"
   
   # Delete specific backup date
   az storage blob delete-batch \
     --account-name <storage-account-name> \
     --source backups \
     --pattern "backups/2023-11-*"
   ```

4. **Upgrade Storage Account**
   - Increase storage quota
   - Consider blob lifecycle management policies

### Getting Help

If issues persist:

1. **Check Documentation**
   - Review [BACKUP_WORKER_CONFIGURATION.md](./BACKUP_WORKER_CONFIGURATION.md)
   - Review [DEPLOYMENT.md](./DEPLOYMENT.md)

2. **Review Logs**
   - Application logs: `docker logs tralivali-api`
   - MongoDB logs: `docker logs mongodb`
   - Azure Activity Log: Check in Azure Portal

3. **Community Support**
   - Open GitHub issue: [TralliValli Issues](https://github.com/optiklab/TralliValli/issues)
   - Provide logs and configuration (redact secrets)

4. **Emergency Contact**
   - Contact system administrators
   - Refer to emergency contact list in disaster recovery plan

---

## Related Documentation

- [Backup Worker Configuration](./BACKUP_WORKER_CONFIGURATION.md) - Detailed BackupWorker configuration
- [Deployment Guide](./DEPLOYMENT.md) - Initial deployment and setup
- [Development Guide](./DEVELOPMENT.md) - Local development setup
- [Azure Blob Storage Configuration](./AZURE_BLOB_STORAGE_CONFIGURATION.md) - Storage setup details

---

## Summary

This guide has covered:

- ✅ **Automatic Backup Schedule**: Daily backups at 3 AM UTC, configurable via cron
- ✅ **Backup File Locations**: Azure Blob Storage with `backups/{date}/` structure
- ✅ **Manual Backup Trigger**: Admin API endpoint for on-demand backups
- ✅ **Restore Procedures**: API-based and manual restore methods
- ✅ **Point-in-Time Recovery**: Daily recovery points, process documented
- ✅ **Disaster Recovery Plan**: Complete procedures for various disaster scenarios

**Key Takeaways:**
- Backups run automatically daily and are retained for 30 days
- Manual backups can be triggered via admin API before critical operations
- Restores can be performed via API or manually using MongoDB tools
- Recovery points are available daily (default) or based on your schedule
- Disaster recovery procedures exist for complete data loss, region outages, and security breaches
- Regular verification and testing ensure backup reliability

For questions or issues, refer to the [Troubleshooting](#troubleshooting) section or open a GitHub issue.
