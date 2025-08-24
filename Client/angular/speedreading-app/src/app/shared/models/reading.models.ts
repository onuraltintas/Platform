export interface TextContent {
  id: string;
  title: string;
  content: string;
  wordCount: number;
  difficultyLevel: number;
  category: string;
  estimatedReadingTime: number;
  language: string;
  
  // Metadata
  author?: string;
  source?: string;
  tags: string[];
  
  // Processing
  sentences: Sentence[];
  paragraphs: Paragraph[];
  chunks: TextChunk[];
}

export interface Sentence {
  id: number;
  text: string;
  startIndex: number;
  endIndex: number;
  wordCount: number;
}

export interface Paragraph {
  id: number;
  sentences: Sentence[];
  startIndex: number;
  endIndex: number;
  wordCount: number;
}

export interface TextChunk {
  id: number;
  words: string[];
  startIndex: number;
  endIndex: number;
  duration: number; // display time in ms
  difficulty: number;
}

export enum ReadingMode {
  CLASSIC = 'classic',
  RSVP = 'rsvp',
  CHUNK = 'chunk',
  GUIDED = 'guided'
}

export interface ReadingSession {
  sessionId: string;
  textId: string;
  userId: string;
  readingMode: ReadingMode;
  startTime: Date;
  endTime?: Date;
  
  // Timing Metrics
  totalDuration: number; // milliseconds
  readingDuration: number; // excluding pauses
  pauseDuration: number;
  
  // Reading Metrics
  wordCount: number;
  wordsPerMinute: number;
  charactersPerMinute: number;
  
  // Interaction Metrics
  pauseCount: number;
  scrollEvents: number;
  regressionCount: number; // geri dönüş sayısı
  
  // Settings used during session
  settings: ReadingSettings;
}

export interface ReadingSettings {
  // Speed Settings
  wordsPerMinute: number;
  chunkSize: number; // for chunk reading
  
  // Visual Settings
  fontSize: number;
  fontFamily: string;
  lineHeight: number;
  backgroundColor: string;
  textColor: string;
  highlightColor: string;
  
  // Behavior Settings
  autoStart: boolean;
  autoPause: boolean;
  showProgress: boolean;
  enableSounds: boolean;
  
  // RSVP Settings
  rsvpFocusPoint: boolean;
  rsvpWordDuration: number; // milliseconds per word
  
  // Chunk Settings
  chunkHighlightDuration: number;
  chunkPauseDuration: number;
  showContext: boolean;
  showFocusPoint: boolean;
  
  // Guided Reading Settings
  highlighterSpeed: number;
  highlighterHeight: number;
  showReadingGuide: boolean;
  showFocusWindow: boolean;
  showGuideLines: boolean;
  
  // Classic Reading Settings
  enableSpeedMode: boolean;
  enableHighlighting: boolean;
  highlightRange: number;
  // Extensions
  bionicEnabled?: boolean;
  // Preferences
  resumeEnabled?: boolean;
  // randomPick kaldırıldı
}

export interface ReadingPerformance {
  sessionId: string;
  timestamp: number;
  currentWPM: number;
  averageWPM: number;
  wordsRead: number;
  totalWords: number;
  progressPercentage: number;
  regressions: number;
  pauses: number;
}

export interface ComprehensionQuestion {
  id: string;
  textId: string;
  type: QuestionType;
  question: string;
  options?: string[]; // for multiple choice
  correctAnswer: string | number;
  difficulty: number;
  points: number;
}

export enum QuestionType {
  MULTIPLE_CHOICE = 'multiple_choice',
  TRUE_FALSE = 'true_false',
  FILL_BLANK = 'fill_blank',
  SHORT_ANSWER = 'short_answer'
}

export interface ComprehensionTest {
  testId: string;
  textId: string;
  questions: ComprehensionQuestion[];
  timeLimit?: number;
  
  // Results
  score?: number;
  answeredQuestions?: number;
  correctAnswers?: number;
  comprehensionRate?: number;
  completedAt?: Date;
}

export interface SessionResults {
  session: ReadingSession;
  comprehensionTest?: ComprehensionTest;
  
  // Calculated Metrics
  efficiency: number; // WPM vs comprehension balance
  consistency: number; // speed consistency throughout session
  improvement: number; // compared to previous sessions
  
  // Recommendations
  recommendedSpeed: number;
  suggestedExercises: string[];
  nextSteps: string[];
}

export interface ReadingGoal {
  id: string;
  userId: string;
  type: GoalType;
  target: number;
  current: number;
  deadline: Date;
  isCompleted: boolean;
}

export enum GoalType {
  READING_SPEED = 'reading_speed',
  COMPREHENSION_RATE = 'comprehension_rate',
  DAILY_WORDS = 'daily_words',
  WEEKLY_SESSIONS = 'weekly_sessions'
}