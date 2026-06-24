# DustBot App Store submission pack

## Product record

- App name: `DustBot`
- Primary category: `Games`
- Secondary category: `Puzzle`
- Version: `1.0.0`
- Build: `1`
- Platform: iPhone
- Minimum OS: iOS 15.0
- Price model: choose Free or Paid; the binary contains no ads or purchases

## Before upload

1. Confirm the bundle identifier is registered to the correct Apple Developer team.
2. Run `DustBot > Build iOS Release`.
3. Open `Build/iOS/Unity-iPhone.xcodeproj` with Xcode 26 or later.
4. Select the team and automatic signing.
5. Archive a generic iOS device build and run Xcode validation.
6. Upload through Xcode Organizer.

## App Store Connect

- Use the files in `Metadata/en-US`.
- Host `privacy-policy.html` and `support.html` at public HTTPS URLs.
- App Privacy answer: **No, we do not collect data from this app.**
- Complete the age-rating questionnaire using `age-rating.md`.
- Encryption: the generated `Info.plist` declares that the app does not use non-exempt encryption.
- Upload final gameplay screenshots at an accepted 6.9-inch iPhone portrait size.

Apple Developer membership, bundle-ID ownership, signing, tax/banking choices, pricing, and the final Submit for Review action require the account holder.
