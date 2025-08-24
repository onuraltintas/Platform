import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { TextContent, TextChunk, Sentence, Paragraph, ComprehensionQuestion, QuestionType } from '../../../shared/models/reading.models';
import { ReadingContentApiService } from './reading-content-api.service';

@Injectable({
  providedIn: 'root'
})
export class TextProcessingService {

  constructor(private contentApi: ReadingContentApiService) {}

  /**
   * Get texts from backend with processing
   */
  getTexts(options?: {
    search?: string;
    difficultyLevel?: string;
    page?: number;
    pageSize?: number;
  }): Observable<TextContent[]> {
    return this.contentApi.getTexts(options).pipe(
      map(texts => texts.map(text => this.processTextStructure(text)))
    );
  }

  /**
   * Get specific text by ID with processing
   */
  getTextById(textId: string): Observable<TextContent | null> {
    return this.contentApi.getTextById(textId).pipe(
      map(text => text ? this.processTextStructure(text) : null)
    );
  }

  /**
   * Get comprehension questions for a text
   */
  getComprehensionQuestions(textId: string): Observable<ComprehensionQuestion[]> {
    return this.contentApi.getQuestionsByTextId(textId);
  }

  /**
   * Process text structure (add sentences, paragraphs, chunks)
   */
  processTextStructure(text: TextContent): TextContent {
    const sentences = this.extractSentences(text.content);
    const paragraphs = this.extractParagraphs(text.content, sentences);
    const chunks = this.createChunks(text.content);

    return {
      ...text,
      sentences,
      paragraphs,
      chunks
    };
  }

  /**
   * Process raw text content into structured format
   */
  processText(rawText: string, textId: string, title: string): TextContent {
    const cleanText = this.cleanText(rawText);
    const sentences = this.extractSentences(cleanText);
    const paragraphs = this.extractParagraphs(cleanText, sentences);
    const wordCount = this.countWords(cleanText);
    const chunks = this.createChunks(cleanText);

    return {
      id: textId,
      title: title,
      content: cleanText,
      wordCount: wordCount,
      difficultyLevel: this.calculateDifficulty(cleanText),
      category: 'General',
      estimatedReadingTime: this.estimateReadingTime(wordCount),
      language: 'tr',
      author: undefined,
      source: undefined,
      tags: [],
      sentences: sentences,
      paragraphs: paragraphs,
      chunks: chunks
    };
  }

  /**
   * Clean and normalize text
   */
  private cleanText(text: string): string {
    return text
      .replace(/\s+/g, ' ') // Multiple spaces to single space
      .replace(/\n\s*\n/g, '\n\n') // Multiple newlines to double newline
      .trim();
  }

  /**
   * Extract sentences from text
   */
  private extractSentences(text: string): Sentence[] {
    const sentences: Sentence[] = [];
    const sentenceRegex = /[.!?]+/g;
    let lastIndex = 0;
    let match;
    let sentenceId = 0;

    while ((match = sentenceRegex.exec(text)) !== null) {
      const endIndex = match.index + match[0].length;
      const sentenceText = text.substring(lastIndex, endIndex).trim();
      
      if (sentenceText.length > 0) {
        sentences.push({
          id: sentenceId++,
          text: sentenceText,
          startIndex: lastIndex,
          endIndex: endIndex,
          wordCount: this.countWords(sentenceText)
        });
      }
      
      lastIndex = endIndex;
    }

    // Handle remaining text if no ending punctuation
    if (lastIndex < text.length) {
      const remainingText = text.substring(lastIndex).trim();
      if (remainingText.length > 0) {
        sentences.push({
          id: sentenceId,
          text: remainingText,
          startIndex: lastIndex,
          endIndex: text.length,
          wordCount: this.countWords(remainingText)
        });
      }
    }

    return sentences;
  }

  /**
   * Extract paragraphs from text
   */
  private extractParagraphs(text: string, sentences: Sentence[]): Paragraph[] {
    const paragraphs: Paragraph[] = [];
    const paragraphTexts = text.split(/\n\s*\n/);
    let currentIndex = 0;
    let paragraphId = 0;

    for (const paragraphText of paragraphTexts) {
      if (paragraphText.trim().length === 0) continue;

      const startIndex = text.indexOf(paragraphText, currentIndex);
      const endIndex = startIndex + paragraphText.length;
      
      // Find sentences that belong to this paragraph
      const paragraphSentences = sentences.filter(sentence => 
        sentence.startIndex >= startIndex && sentence.endIndex <= endIndex
      );

      paragraphs.push({
        id: paragraphId++,
        sentences: paragraphSentences,
        startIndex: startIndex,
        endIndex: endIndex,
        wordCount: this.countWords(paragraphText)
      });

      currentIndex = endIndex;
    }

    return paragraphs;
  }

  /**
   * Create text chunks for chunk reading
   */
  createChunks(text: string, chunkSize: number = 3): TextChunk[] {
    const words = text.split(/\s+/).filter(word => word.length > 0);
    const chunks: TextChunk[] = [];
    let chunkId = 0;
    let currentIndex = 0;

    for (let i = 0; i < words.length; i += chunkSize) {
      const chunkWords = words.slice(i, i + chunkSize);
      const chunkText = chunkWords.join(' ');
      const startIndex = text.indexOf(chunkText, currentIndex);
      const endIndex = startIndex + chunkText.length;

      chunks.push({
        id: chunkId++,
        words: chunkWords,
        startIndex: Math.max(0, startIndex),
        endIndex: endIndex > 0 ? endIndex : currentIndex + chunkText.length,
        duration: this.calculateChunkDuration(chunkWords),
        difficulty: this.calculateChunkDifficulty(chunkWords)
      });

      currentIndex = endIndex > 0 ? endIndex : currentIndex + chunkText.length;
    }

    return chunks;
  }

  /**
   * Count words in text
   */
  private countWords(text: string): number {
    return text.split(/\s+/).filter(word => word.length > 0).length;
  }

  /**
   * Calculate text difficulty (1-10 scale)
   */
  private calculateDifficulty(text: string): number {
    const words = text.split(/\s+/);
    const sentences = text.split(/[.!?]+/).filter(s => s.trim().length > 0);
    
    // Average word length
    const avgWordLength = words.reduce((sum, word) => sum + word.length, 0) / words.length;
    
    // Average sentence length
    const avgSentenceLength = words.length / sentences.length;
    
    // Complex word ratio (words with 3+ syllables, approximated by length)
    const complexWords = words.filter(word => word.length > 6).length;
    const complexWordRatio = complexWords / words.length;
    
    // Simple difficulty calculation
    let difficulty = 1;
    
    if (avgWordLength > 5) difficulty += 2;
    if (avgSentenceLength > 20) difficulty += 2;
    if (complexWordRatio > 0.3) difficulty += 3;
    if (avgSentenceLength > 30) difficulty += 2;
    
    return Math.min(10, Math.max(1, difficulty));
  }

  /**
   * Estimate reading time in minutes
   */
  private estimateReadingTime(wordCount: number, wpm: number = 250): number {
    return Math.ceil(wordCount / wpm);
  }

  /**
   * Calculate chunk display duration based on word complexity
   */
  private calculateChunkDuration(words: string[]): number {
    const baseTime = 800; // base 800ms per chunk
    const wordLengthFactor = words.reduce((sum, word) => sum + word.length, 0) / words.length;
    const complexityMultiplier = 1 + (wordLengthFactor - 4) * 0.1;
    
    return Math.max(400, Math.min(2000, baseTime * complexityMultiplier));
  }

  /**
   * Calculate chunk difficulty
   */
  private calculateChunkDifficulty(words: string[]): number {
    const avgLength = words.reduce((sum, word) => sum + word.length, 0) / words.length;
    
    if (avgLength < 4) return 1;
    if (avgLength < 6) return 2;
    if (avgLength < 8) return 3;
    return 4;
  }

  /**
   * Get word at specific index in text
   */
  getWordAtIndex(text: string, index: number): { word: string; startIndex: number; endIndex: number } | null {
    const words = text.split(/\s+/);
    let currentIndex = 0;
    
    for (const word of words) {
      const wordStart = text.indexOf(word, currentIndex);
      const wordEnd = wordStart + word.length;
      
      if (index >= wordStart && index <= wordEnd) {
        return {
          word: word,
          startIndex: wordStart,
          endIndex: wordEnd
        };
      }
      
      currentIndex = wordEnd;
    }
    
    return null;
  }

  /**
   * Calculate WPM based on time and word count
   */
  calculateWPM(wordCount: number, timeInMilliseconds: number): number {
    const minutes = timeInMilliseconds / (1000 * 60);
    return Math.round(wordCount / minutes);
  }

  /**
   * Get reading progress percentage
   */
  getProgressPercentage(currentIndex: number, totalLength: number): number {
    return Math.round((currentIndex / totalLength) * 100);
  }

  /**
   * Extract keywords from text for comprehension questions
   */
  extractKeywords(text: string, count: number = 10): string[] {
    const words = text.toLowerCase()
      .replace(/[^\w\s]/g, '')
      .split(/\s+/)
      .filter(word => word.length > 3);

    // Simple frequency analysis
    const wordFreq: { [key: string]: number } = {};
    words.forEach(word => {
      wordFreq[word] = (wordFreq[word] || 0) + 1;
    });

    // Sort by frequency and return top keywords
    return Object.entries(wordFreq)
      .sort(([,a], [,b]) => b - a)
      .slice(0, count)
      .map(([word]) => word);
  }

  /**
   * Generate simple comprehension questions
   */
  generateSimpleQuestions(textContent: TextContent): ComprehensionQuestion[] {
    const keywords = this.extractKeywords(textContent.content, 5);
    const questions: ComprehensionQuestion[] = [];

    // Generate basic questions based on keywords
    keywords.forEach((keyword, index) => {
      questions.push({
        id: `q_${textContent.id}_${index}`,
        textId: textContent.id,
        type: QuestionType.MULTIPLE_CHOICE,
        question: `Metinde "${keyword}" kelimesi hangi bağlamda kullanılmıştır?`,
        options: [
          `${keyword} ile ilgili doğru açıklama`,
          `${keyword} ile ilgili yanlış açıklama 1`,
          `${keyword} ile ilgili yanlış açıklama 2`,
          `${keyword} ile ilgili yanlış açıklama 3`
        ],
        correctAnswer: 0,
        difficulty: 2,
        points: 10
      });
    });

    return questions;
  }
}