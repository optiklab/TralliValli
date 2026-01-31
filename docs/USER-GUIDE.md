# TralliValli User Guide

Welcome to TralliValli! This guide will help you get started with our secure, invite-only messaging platform for family and friends.

## Table of Contents

- [Introduction](#introduction)
- [Getting Started](#getting-started)
  - [Registration Process](#registration-process)
  - [Logging In](#logging-in)
- [Using TralliValli](#using-trallivalli)
  - [Sending Messages](#sending-messages)
  - [Creating Conversations](#creating-conversations)
  - [File Sharing](#file-sharing)
  - [QR Code Invitations](#qr-code-invitations)
- [Troubleshooting](#troubleshooting)

## Introduction

TralliValli is a self-hosted, invite-only messaging platform designed for secure communication within your family and friend circles. The platform features:

- **End-to-end encryption** for secure messaging
- **Invite-only access** to maintain privacy
- **File sharing** with support for images, documents, and more
- **Group conversations** to chat with multiple people
- **QR code invitations** for easy onboarding
- **Real-time messaging** with typing indicators and read receipts

## Getting Started

### Registration Process

To join TralliValli, you need a valid invite link from an existing user. Here's how to register:

#### Step 1: Obtain an Invite Link

You'll need to receive an invite link from someone who is already using TralliValli. The invite link can be shared via:
- Email
- Text message
- QR code (scan with your camera)
- Direct URL

Invite links typically look like:
```
https://your-trallivalli-domain.com/invite/abc123-def456-ghi789
```

**Important:** Invite links expire after a set period (1 hour to 1 week, depending on how it was created). Make sure to use your invite link before it expires.

#### Step 2: Access the Registration Page

1. Click on the invite link you received or visit the registration page directly
2. If the link includes the invite token, it will be automatically filled in
3. Otherwise, paste your invite link or token into the "Invite Link" field

#### Step 3: Complete Registration

1. **Enter your invite link** - The system will validate your invite link automatically
   - You'll see a green checkmark (✓) if the invite is valid
   - A red X (✗) appears if the invite is invalid or expired

2. **Provide your email address** - Enter a valid email address
   - This will be used for login and account recovery
   - Email format is automatically validated

3. **Choose a display name** - Enter the name you want others to see
   - This can be your real name or a nickname
   - Other users will see this name in conversations

4. **Click "Create account"** to complete registration

Once registration is successful, you'll be automatically logged in and can start using TralliValli!

### Logging In

TralliValli uses a passwordless authentication system called "Magic Links" for secure and convenient login.

#### Step 1: Go to the Login Page

1. Visit your TralliValli instance URL
2. You'll see the login page with an email input field

#### Step 2: Request a Magic Link

1. Enter your email address (the one you used during registration)
2. Click the "Send Magic Link" button
3. You'll see a confirmation message that the magic link has been sent

#### Step 3: Check Your Email

1. Open your email inbox
2. Look for an email from TralliValli (subject: "Your TralliValli Login Link" or similar)
3. Click the magic link in the email

**Security Note:** Magic links expire after a short period (typically 15 minutes) for security reasons.

#### Step 4: Automatic Login

After clicking the magic link:
1. Your browser will open TralliValli
2. You'll be automatically logged in
3. You can start messaging right away

**Tip:** Keep your session active by using TralliValli regularly. If you're inactive for an extended period, you may need to log in again.

## Using TralliValli

### Sending Messages

TralliValli makes it easy to send messages to your contacts. All messages are end-to-end encrypted for your privacy.

#### Starting a Conversation

1. **Select a conversation** from the conversation list on the left sidebar
2. If you don't have any conversations yet, you'll need to create one first (see [Creating Conversations](#creating-conversations))

#### Composing Messages

1. **Type your message** in the text input field at the bottom of the screen
   - The text field automatically expands as you type
   - Maximum height is limited for comfortable viewing

2. **Format your message** (optional)
   - Press `Enter` to send your message
   - Press `Shift + Enter` to add a new line without sending
   - Use the emoji button (😊) to add emojis to your message

3. **Send your message**
   - Click the send button (paper plane icon) or press `Enter`
   - Your message will be encrypted and sent immediately
   - You'll see your message appear in the conversation thread

#### Message Features

- **Read Receipts**: See when others have read your messages
- **Typing Indicators**: Know when someone is typing a response
- **Timestamps**: Each message shows when it was sent
- **Reply to Messages**: Click the reply icon to respond to specific messages
- **Message Threading**: Replies are visually connected to the original message

#### Emoji Support

1. Click the emoji button (😊) next to the message input
2. An emoji picker will appear
3. Click any emoji to add it to your message
4. The picker closes automatically, or click outside to close it manually

#### Pasting Images

You can quickly share images by pasting them directly:
1. Copy an image to your clipboard (from a screenshot, file browser, etc.)
2. Click in the message input field
3. Press `Ctrl + V` (Windows/Linux) or `Cmd + V` (Mac)
4. The image will be attached to your message
5. Send the message to share the image

### Creating Conversations

TralliValli supports both one-on-one conversations and group chats.

#### Direct Conversations

To start a conversation with a single person:

1. **Look for the new conversation option** in your interface
2. **Select the contact** you want to message
3. The conversation will be created, and you can start sending messages

**Note:** Both you and the other person must be registered users of your TralliValli instance.

#### Group Conversations

To create a group conversation with multiple people:

1. **Access the group creation feature** (typically a "New Group" or "+" button)
2. **Add participants**
   - Select multiple contacts from your contact list
   - You can add or remove participants before creating the group
3. **Name your group** (optional but recommended)
   - Choose a descriptive name so everyone knows what the group is for
   - Examples: "Family Chat", "Weekend Trip Planning", "Book Club"
4. **Create the group**
   - Click the create or confirm button
   - All selected participants will be added to the conversation
   - Everyone can start messaging immediately

#### Group Features

- **Multiple participants**: Add as many people as needed
- **Group name**: Easy identification in your conversation list
- **Participant list**: See who's in the group
- **Equal access**: All members can send messages and share files
- **Persistent history**: Messages are saved for all group members

**Tip:** Use descriptive group names to keep your conversations organized, especially if you have many group chats.

### File Sharing

TralliValli supports secure file sharing with end-to-end encryption for your files.

#### Supported File Types

You can share various types of files:
- **Images**: JPG, PNG, GIF, WebP
- **Documents**: PDF, TXT, DOCX, XLSX
- **Archives**: ZIP, RAR
- **Videos**: MP4, WebM, MOV
- **Audio**: MP3, WAV, OGG
- And more!

#### Uploading Files

There are multiple ways to share files:

**Method 1: Using the Attach Button**
1. Click the attachment button (📎) in the message composer
2. Browse your device and select one or more files
3. The files will be attached to your message
4. Add an optional text message if desired
5. Click send to share the files

**Method 2: Drag and Drop**
1. Open the conversation where you want to share files
2. Drag files from your file browser
3. Drop them onto the message composer area
4. The files will be automatically attached
5. Send your message to share

**Method 3: Paste Images**
1. Copy an image to your clipboard
2. Click in the message input field
3. Press `Ctrl + V` (Windows/Linux) or `Cmd + V` (Mac)
4. The image will be attached
5. Send the message

#### Upload Progress

When uploading files:
- You'll see a **progress bar** showing the upload status
- **File thumbnails** appear for images
- **File metadata** (name, size, type) is displayed
- You can **cancel uploads** if needed before they complete

#### Viewing Shared Files

When someone shares files with you:
1. **Images** display as thumbnails in the conversation
   - Click to view full size
   - Access the media gallery to see all images in a conversation
2. **Documents and other files** show with an icon and filename
   - Click to download the file
   - File size is displayed for your reference

#### File Encryption

All files are encrypted before upload:
- **End-to-end encryption** ensures only conversation participants can access files
- Files are **encrypted locally** on your device before being sent
- The server cannot decrypt or access your files
- Decryption happens automatically when you view files

**Security Tip:** Even though files are encrypted, be mindful of what you share and ensure you trust all conversation participants.

### QR Code Invitations

QR codes make it easy to invite people to TralliValli in person.

#### Generating an Invite QR Code

If you want to invite someone to join TralliValli:

1. **Open the Invite Modal**
   - Look for an "Invite Friends" or similar button in your interface
   - This is typically found in settings or the main menu

2. **Choose Link Expiry**
   - Select how long the invite should be valid:
     - 1 hour (for immediate use)
     - 6 hours (for same-day invites)
     - 24 hours (default, good for next-day use)
     - 3 days (for flexible timing)
     - 1 week (maximum validity)

3. **Generate the Invite**
   - Click "Generate Invite Link"
   - A QR code will appear along with a shareable link

4. **Share the QR Code**
   - Show the QR code to the person you want to invite
   - They can scan it with their phone's camera
   - The invite link will open automatically

#### Scanning a QR Code Invite

If someone shares a QR code invite with you:

1. **Open Your Camera App**
   - Use your phone's built-in camera app (iOS, Android)
   - Or use a dedicated QR code scanner app

2. **Point at the QR Code**
   - Center the QR code in your camera view
   - Hold steady until it's recognized
   - Most modern phones detect QR codes automatically

3. **Tap the Notification**
   - A notification or popup will appear with the invite link
   - Tap it to open the registration page
   - The invite token will be automatically filled in

4. **Complete Registration**
   - Follow the [Registration Process](#registration-process) steps
   - Your invite is already validated, so just add your email and name

#### Alternative: Using the Built-in QR Scanner

TralliValli also has a built-in QR code scanner:

1. **Access the Scanner**
   - Look for a "Scan QR Code" option during registration
   - Or use the QR scanner feature in the app

2. **Grant Camera Permission**
   - Your browser will ask for camera access
   - Click "Allow" to use the scanner
   - This is required for the scanner to work

3. **Scan the Code**
   - Position the QR code within the scanner frame
   - Keep the code well-lit and in focus
   - The scanner will automatically detect and process the code

4. **Automatic Registration**
   - Once scanned, you'll be taken to registration
   - The invite token is automatically applied
   - Complete the remaining registration fields

#### Sharing Invite Links Without QR Codes

You can also share the invite link directly:

1. Generate the invite as described above
2. Click the "Copy" button next to the invite link
3. Share the link via:
   - Email
   - Text message
   - Messaging apps (WhatsApp, Signal, etc.)
   - Any other communication method

**Important:** Remember that invite links expire! Make sure recipients use them before the expiry time you selected.

## Troubleshooting

### Common Issues and Solutions

#### Can't Register: "Invalid or Expired Invite Link"

**Problem:** The invite link isn't working during registration.

**Solutions:**
1. **Check expiry** - Invite links expire after their set duration (1 hour to 1 week)
   - Request a new invite from the person who invited you
   - Ask them to set a longer expiry time if needed

2. **Verify the link** - Make sure you copied the entire invite link
   - The link should be complete with no missing characters
   - Try copying the link again from the source

3. **Clear your browser cache**
   - Cached data might interfere with validation
   - Try registering in a private/incognito browser window

4. **Contact the sender** - The invite may have been deactivated
   - Ask them to generate a new invite for you

#### Magic Link Not Arriving

**Problem:** You requested a login magic link, but it's not in your email.

**Solutions:**
1. **Check your spam/junk folder**
   - Email providers sometimes flag automated emails
   - Look for emails from your TralliValli domain

2. **Wait a few minutes**
   - Email delivery can take 1-5 minutes
   - Don't request multiple links too quickly

3. **Verify your email address**
   - Make sure you entered the correct email
   - Check for typos in the email address

4. **Check email server logs** (if you're self-hosting)
   - Ensure the email service is configured correctly
   - Verify SMTP settings in the server configuration

5. **Request a new link**
   - If more than 10 minutes have passed, request another magic link
   - Previous links expire after 15 minutes anyway

#### Messages Not Sending

**Problem:** Your messages aren't being delivered.

**Solutions:**
1. **Check your internet connection**
   - Make sure you're online
   - Try refreshing the page

2. **Verify connection status**
   - Look for a connection indicator in the interface
   - If disconnected, the app should show a warning

3. **Check message encryption**
   - Ensure encryption keys are properly set up
   - Try refreshing the page to re-establish encryption

4. **Browser console errors**
   - Open browser developer tools (F12)
   - Check the console for any error messages
   - Share these with your administrator if issues persist

5. **Try a different browser**
   - Browser compatibility issues can sometimes occur
   - Test with Chrome, Firefox, or Edge

#### File Upload Failing

**Problem:** Files won't upload or the upload fails partway through.

**Solutions:**
1. **Check file size**
   - Large files may exceed the upload limit
   - Try uploading smaller files or compressing the file first
   - Check with your administrator about file size limits

2. **Verify internet connection**
   - Unstable connections can interrupt uploads
   - Ensure you have a stable connection before uploading large files

3. **Check file type**
   - Some file types might be restricted
   - Try renaming the file with a standard extension (e.g., .jpg, .pdf)

4. **Clear browser cache**
   - Cached data might interfere with uploads
   - Clear cache and try again

5. **Try smaller files first**
   - Test with a small file (< 1MB) to verify upload functionality
   - If small files work, the issue is likely related to file size or connection

#### Cannot Scan QR Code

**Problem:** The QR code scanner isn't working.

**Solutions:**
1. **Grant camera permissions**
   - Your browser needs permission to access the camera
   - Click "Allow" when prompted
   - Check browser settings if you previously denied permission

2. **Check camera availability**
   - Ensure your device has a working camera
   - Make sure no other app is using the camera

3. **Ensure good lighting**
   - QR codes need adequate lighting to scan
   - Avoid glare or shadows on the QR code
   - Hold your device steady

4. **Try the camera app instead**
   - Use your phone's built-in camera app
   - Most modern phones recognize QR codes automatically
   - Tap the notification to open the invite link

5. **Use the link directly**
   - Ask the sender to share the text invite link instead
   - Copy and paste it into the registration form

#### Messages Not Decrypting

**Problem:** You see encrypted messages that won't decrypt, or you see an error message.

**Solutions:**
1. **Refresh the page**
   - Encryption keys might not have loaded properly
   - A refresh usually resolves this

2. **Clear browser data**
   - Clear cookies and cache for the TralliValli domain
   - Log in again to re-establish encryption

3. **Check conversation participation**
   - You must be a participant in the conversation to decrypt messages
   - If you were recently added, older messages may not be decryptable

4. **Verify encryption service**
   - Ensure the encryption service is running properly
   - Contact your administrator if issues persist

#### App Won't Load or Keeps Disconnecting

**Problem:** TralliValli doesn't load properly or frequently disconnects.

**Solutions:**
1. **Check server status**
   - Verify the TralliValli server is running
   - Contact your administrator if the service is down

2. **Clear browser cache and cookies**
   - Old cached data can cause loading issues
   - Clear all data for the TralliValli domain

3. **Try a different browser**
   - Test with another browser to rule out browser-specific issues
   - Supported browsers: Chrome, Firefox, Edge, Safari

4. **Check browser console**
   - Open developer tools (F12)
   - Look for error messages in the console
   - Share these with your administrator

5. **Disable browser extensions**
   - Ad blockers or privacy extensions might interfere
   - Try disabling extensions temporarily
   - Add TralliValli to your extension's allowlist

6. **Check WebSocket support**
   - TralliValli uses WebSockets for real-time communication
   - Ensure your network doesn't block WebSocket connections
   - Corporate firewalls sometimes block WebSockets

### Getting Additional Help

If you continue to experience issues:

1. **Contact your TralliValli administrator**
   - They can check server logs and configuration
   - Provide them with:
     - The exact error message
     - What you were doing when the error occurred
     - Your browser and operating system version
     - Screenshots if applicable

2. **Check the server documentation**
   - Review DEVELOPMENT.md for technical details
   - Check DEPLOYMENT.md for server configuration issues

3. **Report bugs**
   - If you've found a bug, report it to your administrator
   - Include detailed steps to reproduce the issue
   - Provide browser console logs if possible

4. **Community support**
   - Check if your organization has a support channel
   - Ask other users if they've experienced similar issues

---

## Additional Tips

### Best Practices

- **Keep your email address up to date** - This is essential for receiving magic links
- **Use descriptive group names** - Makes it easier to find conversations
- **Set appropriate invite expiry times** - Balance convenience with security
- **Verify recipients before sharing files** - Even though files are encrypted
- **Log out on shared devices** - Protect your privacy on public or shared computers

### Privacy and Security

- **End-to-end encryption** - All messages and files are encrypted
- **Invite-only access** - Only invited users can join your instance
- **Passwordless authentication** - Magic links are more secure than passwords
- **Session management** - Sessions expire after inactivity for security
- **Local encryption** - Encryption happens on your device before transmission

### Staying Connected

- **Browser notifications** - Enable notifications to stay updated (if available)
- **Keep the app open** - Better real-time experience when the app is active
- **Check regularly** - Messages are delivered instantly when you're online

---

**Welcome to TralliValli!** We hope this guide helps you get started. Enjoy secure, private communication with your friends and family!
